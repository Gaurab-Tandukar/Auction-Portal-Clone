using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Auction_Portal_Clone.Migrations
{
    /// <inheritdoc />
    public partial class AddCollateralCategoryToAuctionItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CollateralCategory",
                table: "AuctionItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_AuctionItems_CollateralCategory_Status_AuctionEndDate",
                table: "AuctionItems",
                columns: new[] { "CollateralCategory", "Status", "AuctionEndDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AuctionItems_CollateralCategory_Status_AuctionEndDate",
                table: "AuctionItems");

            migrationBuilder.DropColumn(
                name: "CollateralCategory",
                table: "AuctionItems");
        }
    }
}
