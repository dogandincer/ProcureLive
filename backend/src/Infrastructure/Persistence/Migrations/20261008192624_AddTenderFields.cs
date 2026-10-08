using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcureLive.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTenderFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowAlternativeProducts",
                table: "Auctions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AllowPartialBids",
                table: "Auctions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "DeliveryTerm",
                table: "Auctions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ExtensionDurationMinutes",
                table: "Auctions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxExtensionMinutes",
                table: "Auctions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaymentTerm",
                table: "Auctions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferenceCode",
                table: "Auctions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RequestedDeliveryDate",
                table: "AuctionItems",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Specification",
                table: "AuctionItems",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AuctionAttachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AuctionId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuctionAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuctionAttachments_Auctions_AuctionId",
                        column: x => x.AuctionId,
                        principalTable: "Auctions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Auctions_ReferenceCode",
                table: "Auctions",
                column: "ReferenceCode");

            migrationBuilder.CreateIndex(
                name: "IX_AuctionAttachments_AuctionId",
                table: "AuctionAttachments",
                column: "AuctionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuctionAttachments");

            migrationBuilder.DropIndex(
                name: "IX_Auctions_ReferenceCode",
                table: "Auctions");

            migrationBuilder.DropColumn(
                name: "AllowAlternativeProducts",
                table: "Auctions");

            migrationBuilder.DropColumn(
                name: "AllowPartialBids",
                table: "Auctions");

            migrationBuilder.DropColumn(
                name: "DeliveryTerm",
                table: "Auctions");

            migrationBuilder.DropColumn(
                name: "ExtensionDurationMinutes",
                table: "Auctions");

            migrationBuilder.DropColumn(
                name: "MaxExtensionMinutes",
                table: "Auctions");

            migrationBuilder.DropColumn(
                name: "PaymentTerm",
                table: "Auctions");

            migrationBuilder.DropColumn(
                name: "ReferenceCode",
                table: "Auctions");

            migrationBuilder.DropColumn(
                name: "RequestedDeliveryDate",
                table: "AuctionItems");

            migrationBuilder.DropColumn(
                name: "Specification",
                table: "AuctionItems");
        }
    }
}
