using System.Net;
using Auction_Portal_Clone.Data;
using Auction_Portal_Clone.DTO;
using Auction_Portal_Clone.Models;
using Auction_Portal_Clone.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Auction_Portal_Clone.Services.Implementation
{
    public class AuctionWinnerService : IAuctionWinnerService
    {
        private readonly AuctionDbContext _db;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<AuctionWinnerService> _logger;
        private readonly IConfiguration _config;

        private static readonly SemaphoreSlim _lock = new(1, 1);

        public AuctionWinnerService(
            AuctionDbContext db,
            IEmailSender emailSender,
            ILogger<AuctionWinnerService> logger,
            IConfiguration config)
        {
            _db = db;
            _emailSender = emailSender;
            _logger = logger;
            _config = config;
        }

        public async Task<ServiceResult<AuctionWinnerProcessResultDTO>> ProcessEndedAuctionsAsync(CancellationToken cancellationToken = default)
        {
            await _lock.WaitAsync(cancellationToken);
            try
            {
                var now = DateTime.UtcNow;
                var candidateAuctionIds = await _db.AuctionItems
                    .Where(a => a.Status != AuctionStatus.Draft
                                && a.AuctionEndDate <= now
                                && a.FinalStatus == AuctionFinalStatus.Pending
                                && a.WinnerDeterminedAt == null)
                    .OrderBy(a => a.AuctionEndDate)
                    .Select(a => a.Id)
                    .ToListAsync(cancellationToken);

                var processResult = new AuctionWinnerProcessResultDTO
                {
                    TotalChecked = candidateAuctionIds.Count,
                    ProcessedAt = now
                };

                if (candidateAuctionIds.Count == 0)
                {
                    return ServiceResult<AuctionWinnerProcessResultDTO>.Success(processResult);
                }

                _logger.LogInformation("Found {Count} ended auctions awaiting winner determination.", candidateAuctionIds.Count);

                foreach (var auctionId in candidateAuctionIds)
                {
                    var result = await DetermineWinnerForAuctionInternalAsync(auctionId, cancellationToken);
                    if (result.Succeeded && result.Data != null)
                    {
                        processResult.Results.Add(result.Data);
                        if (result.Data.FinalStatus == AuctionFinalStatus.Sold)
                            processResult.TotalSold++;
                        else if (result.Data.FinalStatus == AuctionFinalStatus.Unsold)
                            processResult.TotalUnsold++;

                        if (result.Data.EmailSent)
                            processResult.TotalEmailsSent++;
                    }
                    else
                    {
                        processResult.TotalErrors++;
                        _logger.LogWarning("Auction {AuctionId} could not be finalized: {Message}", auctionId, result.ErrorMessage);
                    }
                }

                _logger.LogInformation("Finalization completed. Sold: {Sold}, Unsold: {Unsold}, Emails Sent: {Emails}",
                    processResult.TotalSold, processResult.TotalUnsold, processResult.TotalEmailsSent);

                return ServiceResult<AuctionWinnerProcessResultDTO>.Success(processResult);
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task<ServiceResult<AuctionWinnerResultDTO>> DetermineWinnerForAuctionAsync(int auctionItemId, CancellationToken cancellationToken = default)
        {
            await _lock.WaitAsync(cancellationToken);
            try
            {
                return await DetermineWinnerForAuctionInternalAsync(auctionItemId, cancellationToken);
            }
            finally
            {
                _lock.Release();
            }
        }

        private async Task<ServiceResult<AuctionWinnerResultDTO>> DetermineWinnerForAuctionInternalAsync(int auctionItemId, CancellationToken cancellationToken)
        {
            var item = await _db.AuctionItems
                .Include(a => a.Bids)
                    .ThenInclude(b => b.User)
                .FirstOrDefaultAsync(a => a.Id == auctionItemId, cancellationToken);

            if (item == null)
                return ServiceResult<AuctionWinnerResultDTO>.Failure($"Auction item #{auctionItemId} was not found.");

            // Idempotency check: if already finalized, do not re-process or re-award
            if (item.WinnerDeterminedAt != null || item.FinalStatus != AuctionFinalStatus.Pending)
            {
                var existingWinner = item.Bids.FirstOrDefault(b => b.Id == item.WinningBidId)?.User;
                return ServiceResult<AuctionWinnerResultDTO>.Success(new AuctionWinnerResultDTO
                {
                    AuctionItemId = item.Id,
                    Title = item.Title,
                    FinalStatus = item.FinalStatus,
                    WinnerUserId = item.WinnerUserId,
                    WinnerName = existingWinner?.FullName,
                    WinnerEmail = existingWinner?.Email,
                    WinningAmount = item.WinningAmount,
                    WinningBidId = item.WinningBidId,
                    EmailSent = item.WinnerNotifiedAt != null,
                    Message = "Auction was already finalized."
                });
            }

            var now = DateTime.UtcNow;

            // Only end auctions that have actually reached their EndDate
            if (now < item.AuctionEndDate)
            {
                return ServiceResult<AuctionWinnerResultDTO>.Failure(
                    $"Auction #{auctionItemId} has not reached its end date ({item.AuctionEndDate:yyyy-MM-dd HH:mm:ss} UTC).");
            }

            // Find qualifying bids: meeting or exceeding reserve price
            var qualifyingBids = item.Bids
                .Where(b => b.OfferedAmount >= item.ReservePrice)
                .OrderByDescending(b => b.OfferedAmount)
                .ThenBy(b => b.SubmittedAt)
                .ToList();

            bool emailSent = false;
            string outcomeMessage;

            if (qualifyingBids.Count > 0)
            {
                var winningBid = qualifyingBids.First();
                var winner = winningBid.User;

                item.FinalStatus = AuctionFinalStatus.Sold;
                item.Status = AuctionStatus.Closed;
                item.WinnerUserId = winningBid.UserId;
                item.WinningBidId = winningBid.Id;
                item.WinningAmount = winningBid.OfferedAmount;
                item.WinnerDeterminedAt = now;

                outcomeMessage = $"Winner declared: {winner?.FullName} ({winningBid.OfferedAmount:C}).";

                // Save determination to database first to guarantee persistence
                await _db.SaveChangesAsync(cancellationToken);

                // Notify winner via email if not already sent
                if (item.WinnerNotifiedAt == null && winner != null && !string.IsNullOrWhiteSpace(winner.Email))
                {
                    try
                    {
                        await SendWinnerEmailNotificationAsync(item, winner, winningBid);
                        item.WinnerNotifiedAt = DateTime.UtcNow;
                        await _db.SaveChangesAsync(cancellationToken);
                        emailSent = true;
                        _logger.LogInformation("Winning email successfully sent to {Email} for Auction #{AuctionId}.", winner.Email, item.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send winning notification email to {Email} for Auction #{AuctionId}. Determination is preserved.", winner.Email, item.Id);
                    }
                }

                return ServiceResult<AuctionWinnerResultDTO>.Success(new AuctionWinnerResultDTO
                {
                    AuctionItemId = item.Id,
                    Title = item.Title,
                    FinalStatus = item.FinalStatus,
                    WinnerUserId = item.WinnerUserId,
                    WinnerName = winner?.FullName,
                    WinnerEmail = winner?.Email,
                    WinningAmount = item.WinningAmount,
                    WinningBidId = item.WinningBidId,
                    EmailSent = emailSent,
                    Message = outcomeMessage
                });
            }
            else
            {
                // No bids met reserve price or no bids were placed at all
                item.FinalStatus = AuctionFinalStatus.Unsold;
                item.Status = AuctionStatus.Closed;
                item.WinnerUserId = null;
                item.WinningBidId = null;
                item.WinningAmount = null;
                item.WinnerDeterminedAt = now;

                outcomeMessage = item.Bids.Count == 0
                    ? "Auction ended with no bids submitted (Unsold)."
                    : $"Auction ended with {item.Bids.Count} bids, but none met the reserve price of {item.ReservePrice:C} (Unsold).";

                await _db.SaveChangesAsync(cancellationToken);

                return ServiceResult<AuctionWinnerResultDTO>.Success(new AuctionWinnerResultDTO
                {
                    AuctionItemId = item.Id,
                    Title = item.Title,
                    FinalStatus = item.FinalStatus,
                    EmailSent = false,
                    Message = outcomeMessage
                });
            }
        }

        private async Task SendWinnerEmailNotificationAsync(AuctionItem item, User winner, Bid winningBid)
        {
            var branchContact = _config["AuctionSettings:BankBranchContact"] ?? "Siddhartha Bank Special Assets & Recovery Department, Head Office, Hattisar, Kathmandu";
            var contactPhone = _config["AuctionSettings:SupportPhone"] ?? "+977-1-4444444";
            var contactEmail = _config["AuctionSettings:SupportEmail"] ?? "auction-support@siddharthabank.com";
            var claimWindowDays = _config["AuctionSettings:ClaimWindowDays"] ?? "7";

            var subject = $"🎉 Congratulations! You Won Auction #{item.Id} - {item.Title}";

            var html = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f8f7f4; margin: 0; padding: 24px; color: #212529; }}
        .container {{ max-width: 600px; margin: 0 auto; background: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 4px 20px rgba(0,0,0,0.06); border: 1px solid #e9ecef; }}
        .header {{ background: linear-gradient(135deg, #ff6b57 0%, #d94b37 100%); padding: 32px 24px; text-align: center; color: #ffffff; }}
        .header h1 {{ margin: 0 0 8px 0; font-size: 24px; font-weight: 700; }}
        .header p {{ margin: 0; font-size: 14px; opacity: 0.9; letter-spacing: 0.05em; text-transform: uppercase; }}
        .content {{ padding: 32px 28px; }}
        .greeting {{ font-size: 18px; font-weight: 600; margin-bottom: 12px; color: #1a1a1a; }}
        .summary-card {{ background: #fff8f6; border: 1.5px solid #ffded8; border-radius: 12px; padding: 20px; margin: 24px 0; }}
        .summary-row {{ display: flex; justify-content: space-between; margin-bottom: 8px; font-size: 14px; }}
        .summary-label {{ color: #6c757d; }}
        .summary-value {{ font-weight: 600; color: #212529; text-align: right; }}
        .price-highlight {{ font-size: 22px; font-weight: 700; color: #d94b37; text-align: right; }}
        .steps-box {{ background: #fdfdfd; border-left: 4px solid #ff6b57; border-radius: 4px; padding: 16px 20px; margin: 24px 0; }}
        .steps-box h3 {{ margin: 0 0 12px 0; font-size: 16px; font-weight: 700; color: #1a1a1a; }}
        .steps-box ol {{ margin: 0; padding-left: 20px; font-size: 14px; line-height: 1.6; color: #495057; }}
        .steps-box li {{ margin-bottom: 8px; }}
        .footer {{ background: #f8f9fa; padding: 20px 24px; text-align: center; font-size: 12px; color: #6c757d; border-top: 1px solid #e9ecef; }}
        .btn {{ display: inline-block; background: #ff6b57; color: #ffffff !important; padding: 12px 28px; border-radius: 30px; text-decoration: none; font-weight: 700; font-size: 14px; margin-top: 16px; text-align: center; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <p>Siddhartha Bank • Official Auction Notice</p>
            <h1>🎉 Congratulations! You Won!</h1>
        </div>
        <div class='content'>
            <div class='greeting'>Dear {WebUtility.HtmlEncode(winner.FullName)},</div>
            <p style='line-height: 1.6; color: #4a4a4a;'>
                We are pleased to inform you that your offer has emerged as the winning qualifying bid for the following bank asset.
            </p>

            <div class='summary-card'>
                <div class='summary-row'>
                    <span class='summary-label'>Auction Asset:</span>
                    <span class='summary-value'>{WebUtility.HtmlEncode(item.Title)}</span>
                </div>
                <div class='summary-row'>
                    <span class='summary-label'>Item Reference:</span>
                    <span class='summary-value'>AUC-{item.Id:D5}</span>
                </div>
                <div class='summary-row'>
                    <span class='summary-label'>Reserve Price:</span>
                    <span class='summary-value'>Rs. {item.ReservePrice:N2}</span>
                </div>
                <hr style='border: 0; border-top: 1px dashed #f0a89a; margin: 12px 0;' />
                <div class='summary-row' style='align-items: baseline;'>
                    <span class='summary-label' style='font-weight: bold; color: #212529;'>Your Winning Bid:</span>
                    <span class='price-highlight'>Rs. {winningBid.OfferedAmount:N2} NPR</span>
                </div>
                <div class='summary-row'>
                    <span class='summary-label'>Bid Reference:</span>
                    <span class='summary-value'>BID-{winningBid.Id:D5}</span>
                </div>
            </div>

            <div class='steps-box'>
                <h3>Next Steps & Claim Instructions</h3>
                <ol>
                    <li><strong>Contact Within {claimWindowDays} Days:</strong> Please contact or visit Siddhartha Bank within {claimWindowDays} business days to formalize the sale deed and escrow deposit.</li>
                    <li><strong>Bring Required Identification:</strong> Bring original government-issued photo ID (Citizenship Card, Passport, or National ID) matching your registered profile.</li>
                    <li><strong>Reference Information:</strong> Mention Auction ID <code>AUC-{item.Id:D5}</code> and Bid ID <code>BID-{winningBid.Id:D5}</code> when speaking with the auction desk.</li>
                    <li><strong>Bank Branch Location:</strong> {WebUtility.HtmlEncode(branchContact)}.</li>
                </ol>
            </div>

            <p style='font-size: 13px; color: #6c757d; line-height: 1.5;'>
                If you have any questions or require special arrangements, please reach out to our recovery & auction desk at <strong>{contactPhone}</strong> or email <strong>{contactEmail}</strong>.
            </p>
        </div>
        <div class='footer'>
            &copy; {DateTime.UtcNow.Year} Siddhartha Bank Limited. All rights reserved.<br />
            This is an automated operational notification. Please do not reply directly to this email.
        </div>
    </div>
</body>
</html>";

            await _emailSender.SendEmailAsync(winner.Email!, subject, html);
        }
    }
}
