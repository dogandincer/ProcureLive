using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcureLive.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserLanguagePreference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Language",
                table: "NotificationChannelLogs",
                type: "character varying(5)",
                maxLength: 5,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreferredLanguage",
                table: "AspNetUsers",
                type: "character varying(5)",
                maxLength: 5,
                nullable: false,
                defaultValue: "tr");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Language",
                table: "NotificationChannelLogs");

            migrationBuilder.DropColumn(
                name: "PreferredLanguage",
                table: "AspNetUsers");
        }
    }
}
