using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcureLive.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAuctionStartingReminder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "StartingReminderSentAt",
                table: "Auctions",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StartingReminderSentAt",
                table: "Auctions");
        }
    }
}
