using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcureLive.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddErpIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ErpOrderDocNo",
                table: "PurchaseOrders",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ErpItemCode",
                table: "Products",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ErpItemId",
                table: "Products",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ErpEntityCode",
                table: "Companies",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ErpEntityId",
                table: "Companies",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ErpOfferLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AuctionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocNo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ErpOfferMId = table.Column<int>(type: "integer", nullable: true),
                    LineMapJson = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastRevisedBidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErpOfferLinks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ErpSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Mode = table.Column<int>(type: "integer", nullable: false),
                    GetServiceUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SaveServiceUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Username = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    PasswordProtected = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CoCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BranchCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OfferDocTraCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OrderDocTraCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DocNoPrefix = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    EntityCodePrefix = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    ExportNewSuppliers = table.Column<bool>(type: "boolean", nullable: false),
                    ExportNewProducts = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErpSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ErpSyncJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AuctionId = table.Column<Guid>(type: "uuid", nullable: true),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: true),
                    PurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    Summary = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    RequestXml = table.Column<string>(type: "text", nullable: true),
                    ResponseText = table.Column<string>(type: "text", nullable: true),
                    Message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    NextAttemptAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErpSyncJobs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Products_ErpItemCode",
                table: "Products",
                column: "ErpItemCode",
                unique: true,
                filter: "\"ErpItemCode\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_ErpEntityCode",
                table: "Companies",
                column: "ErpEntityCode",
                unique: true,
                filter: "\"ErpEntityCode\" IS NOT NULL AND NOT \"IsDeleted\"");

            migrationBuilder.CreateIndex(
                name: "IX_ErpOfferLinks_AuctionId_CompanyId",
                table: "ErpOfferLinks",
                columns: new[] { "AuctionId", "CompanyId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ErpSyncJobs_AuctionId",
                table: "ErpSyncJobs",
                column: "AuctionId");

            migrationBuilder.CreateIndex(
                name: "IX_ErpSyncJobs_CompanyId",
                table: "ErpSyncJobs",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_ErpSyncJobs_PurchaseOrderId",
                table: "ErpSyncJobs",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ErpSyncJobs_Status_NextAttemptAt",
                table: "ErpSyncJobs",
                columns: new[] { "Status", "NextAttemptAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ErpOfferLinks");

            migrationBuilder.DropTable(
                name: "ErpSettings");

            migrationBuilder.DropTable(
                name: "ErpSyncJobs");

            migrationBuilder.DropIndex(
                name: "IX_Products_ErpItemCode",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Companies_ErpEntityCode",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "ErpOrderDocNo",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "ErpItemCode",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ErpItemId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ErpEntityCode",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "ErpEntityId",
                table: "Companies");
        }
    }
}
