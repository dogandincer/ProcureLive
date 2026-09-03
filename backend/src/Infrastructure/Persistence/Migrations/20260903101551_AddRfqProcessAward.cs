using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcureLive.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRfqProcessAward : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AwardNotes",
                table: "RfqProcesses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AwardedAt",
                table: "RfqProcesses",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WinningQuoteId",
                table: "RfqProcesses",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RfqProcesses_WinningQuoteId",
                table: "RfqProcesses",
                column: "WinningQuoteId");

            migrationBuilder.AddForeignKey(
                name: "FK_RfqProcesses_RfqQuotes_WinningQuoteId",
                table: "RfqProcesses",
                column: "WinningQuoteId",
                principalTable: "RfqQuotes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RfqProcesses_RfqQuotes_WinningQuoteId",
                table: "RfqProcesses");

            migrationBuilder.DropIndex(
                name: "IX_RfqProcesses_WinningQuoteId",
                table: "RfqProcesses");

            migrationBuilder.DropColumn(
                name: "AwardNotes",
                table: "RfqProcesses");

            migrationBuilder.DropColumn(
                name: "AwardedAt",
                table: "RfqProcesses");

            migrationBuilder.DropColumn(
                name: "WinningQuoteId",
                table: "RfqProcesses");
        }
    }
}
