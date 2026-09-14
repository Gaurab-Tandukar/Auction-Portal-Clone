using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Auction_Portal_Clone.Migrations
{
    /// <inheritdoc />
    public partial class AddAuctionWinnerFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FinalStatus",
                table: "AuctionItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "WinnerDeterminedAt",
                table: "AuctionItems",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "WinnerNotifiedAt",
                table: "AuctionItems",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WinnerUserId",
                table: "AuctionItems",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WinningAmount",
                table: "AuctionItems",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WinningBidId",
                table: "AuctionItems",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuctionItems_FinalStatus_AuctionEndDate",
                table: "AuctionItems",
                columns: new[] { "FinalStatus", "AuctionEndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_AuctionItems_WinnerUserId",
                table: "AuctionItems",
                column: "WinnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuctionItems_WinningBidId",
                table: "AuctionItems",
                column: "WinningBidId");

            migrationBuilder.AddForeignKey(
                name: "FK_AuctionItems_AspNetUsers_WinnerUserId",
                table: "AuctionItems",
                column: "WinnerUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AuctionItems_Bids_WinningBidId",
                table: "AuctionItems",
                column: "WinningBidId",
                principalTable: "Bids",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuctionItems_AspNetUsers_WinnerUserId",
                table: "AuctionItems");

            migrationBuilder.DropForeignKey(
                name: "FK_AuctionItems_Bids_WinningBidId",
                table: "AuctionItems");

            migrationBuilder.DropIndex(
                name: "IX_AuctionItems_FinalStatus_AuctionEndDate",
                table: "AuctionItems");

            migrationBuilder.DropIndex(
                name: "IX_AuctionItems_WinnerUserId",
                table: "AuctionItems");

            migrationBuilder.DropIndex(
                name: "IX_AuctionItems_WinningBidId",
                table: "AuctionItems");

            migrationBuilder.DropColumn(
                name: "FinalStatus",
                table: "AuctionItems");

            migrationBuilder.DropColumn(
                name: "WinnerDeterminedAt",
                table: "AuctionItems");

            migrationBuilder.DropColumn(
                name: "WinnerNotifiedAt",
                table: "AuctionItems");

            migrationBuilder.DropColumn(
                name: "WinnerUserId",
                table: "AuctionItems");

            migrationBuilder.DropColumn(
                name: "WinningAmount",
                table: "AuctionItems");

            migrationBuilder.DropColumn(
                name: "WinningBidId",
                table: "AuctionItems");
        }
    }
}
