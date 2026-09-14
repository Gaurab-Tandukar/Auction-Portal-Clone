using Auction_Portal_Clone.Services.Interfaces;

namespace Auction_Portal_Clone.Services.Implementation
{
    public class AuctionWinnerHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AuctionWinnerHostedService> _logger;
        private readonly IConfiguration _config;

        public AuctionWinnerHostedService(
            IServiceScopeFactory scopeFactory,
            ILogger<AuctionWinnerHostedService> logger,
            IConfiguration config)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _config = config;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("AuctionWinnerHostedService is starting.");

            // Configurable interval in seconds; defaults to 60 seconds
            var intervalSeconds = _config.GetValue<int>("AuctionSettings:WinnerCheckIntervalSeconds", 60);
            if (intervalSeconds < 5)
                intervalSeconds = 5;

            var period = TimeSpan.FromSeconds(intervalSeconds);

            // Give the application startup a brief moment before the first run
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            using var timer = new PeriodicTimer(period);

            // Execute immediately on startup, then periodic
            await RunWinnerCheckAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RunWinnerCheckAsync(stoppingToken);
            }

            _logger.LogInformation("AuctionWinnerHostedService is stopping.");
        }

        private async Task RunWinnerCheckAsync(CancellationToken cancellationToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var winnerService = scope.ServiceProvider.GetRequiredService<IAuctionWinnerService>();
                var result = await winnerService.ProcessEndedAuctionsAsync(cancellationToken);

                if (result.Succeeded && result.Data != null && result.Data.TotalChecked > 0)
                {
                    _logger.LogInformation(
                        "Periodic winner check finished: {Checked} checked, {Sold} sold, {Unsold} unsold, {Emails} emails sent.",
                        result.Data.TotalChecked, result.Data.TotalSold, result.Data.TotalUnsold, result.Data.TotalEmailsSent);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Graceful shutdown
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled error occurred during background auction winner check.");
            }
        }
    }
}
