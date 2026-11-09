using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcureLive.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailSettingsAndDelivery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AttemptCount",
                table: "NotificationChannelLogs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextAttemptAt",
                table: "NotificationChannelLogs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Recipient",
                table: "NotificationChannelLogs",
                type: "character varying(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EmailNotificationsEnabled",
                table: "AspNetUsers",
                type: "boolean",
                nullable: false,
                // Mevcut kullanıcılar bildirim e-postalarını varsayılan olarak alır (Profil'den kapatabilir).
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "EmailSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    Host = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Port = table.Column<int>(type: "integer", nullable: false),
                    Security = table.Column<int>(type: "integer", nullable: false),
                    Username = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    SmtpPasswordProtected = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    FromAddress = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    FromName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    ReplyTo = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    DisabledNotificationTypes = table.Column<int[]>(type: "integer[]", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailSettings", x => x.Id);
                });

            // Bu sürüme kadar e-postalar yalnızca loglanıyordu (LoggingEmailSender) ama kayıtları "Sent" (1) işaretleniyordu;
            // gönderim kayıtları ekranında gerçekten gönderilmiş gibi görünmesinler. Channel 1 = Email, Status 3 = Skipped.
            migrationBuilder.Sql("""
                UPDATE "NotificationChannelLogs"
                SET "Status" = 3, "ErrorMessage" = 'SMTP kurulmadan önce oluştu; yalnızca sunucu loguna yazıldı.'
                WHERE "Channel" = 1 AND "Recipient" IS NULL AND "Status" IN (0, 1);
                """);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationChannelLogs_Channel_Status",
                table: "NotificationChannelLogs",
                columns: new[] { "Channel", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailSettings");

            migrationBuilder.DropIndex(
                name: "IX_NotificationChannelLogs_Channel_Status",
                table: "NotificationChannelLogs");

            migrationBuilder.DropColumn(
                name: "AttemptCount",
                table: "NotificationChannelLogs");

            migrationBuilder.DropColumn(
                name: "NextAttemptAt",
                table: "NotificationChannelLogs");

            migrationBuilder.DropColumn(
                name: "Recipient",
                table: "NotificationChannelLogs");

            migrationBuilder.DropColumn(
                name: "EmailNotificationsEnabled",
                table: "AspNetUsers");
        }
    }
}
