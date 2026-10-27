using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcureLive.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPreOfferBidsAndProcessWorkspaces : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "WorkspaceId",
                table: "RfqProcesses",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WorkspaceId",
                table: "RfiProcesses",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PreOfferAppliedAt",
                table: "ParticipationRequests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BidId",
                table: "ParticipationRequestLines",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RfqProcesses_WorkspaceId",
                table: "RfqProcesses",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_RfiProcesses_WorkspaceId",
                table: "RfiProcesses",
                column: "WorkspaceId");

            migrationBuilder.AddForeignKey(
                name: "FK_RfiProcesses_Workspaces_WorkspaceId",
                table: "RfiProcesses",
                column: "WorkspaceId",
                principalTable: "Workspaces",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_RfqProcesses_Workspaces_WorkspaceId",
                table: "RfqProcesses",
                column: "WorkspaceId",
                principalTable: "Workspaces",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // Mevcut RFI/RFQ süreçleri: sahip firmanın varsayılan (yoksa ilk) aktif işyeri (ihalelerle aynı kural).
            foreach (string table in new[] { "RfiProcesses", "RfqProcesses" })
            {
                migrationBuilder.Sql($"""
                    UPDATE "{table}" p SET "WorkspaceId" = (
                        SELECT w."Id" FROM "Workspaces" w
                        WHERE w."FirmId" = p."OwnerCompanyId" AND w."IsActive"
                        ORDER BY w."IsDefault" DESC, w."CreatedAt"
                        LIMIT 1)
                    WHERE p."WorkspaceId" IS NULL;
                    """);
            }

            // Bu sürümden önce kabul edilmiş ve ihalesi canlıya geçmiş/kapanmış talepler geriye dönük teklife çevrilmez.
            migrationBuilder.Sql("""
                UPDATE "ParticipationRequests" r SET "PreOfferAppliedAt" = r."DecidedAt"
                FROM "Auctions" a
                WHERE r."AuctionId" = a."Id" AND r."Status" = 2 AND a."Status" NOT IN (0, 1);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RfiProcesses_Workspaces_WorkspaceId",
                table: "RfiProcesses");

            migrationBuilder.DropForeignKey(
                name: "FK_RfqProcesses_Workspaces_WorkspaceId",
                table: "RfqProcesses");

            migrationBuilder.DropIndex(
                name: "IX_RfqProcesses_WorkspaceId",
                table: "RfqProcesses");

            migrationBuilder.DropIndex(
                name: "IX_RfiProcesses_WorkspaceId",
                table: "RfiProcesses");

            migrationBuilder.DropColumn(
                name: "WorkspaceId",
                table: "RfqProcesses");

            migrationBuilder.DropColumn(
                name: "WorkspaceId",
                table: "RfiProcesses");

            migrationBuilder.DropColumn(
                name: "PreOfferAppliedAt",
                table: "ParticipationRequests");

            migrationBuilder.DropColumn(
                name: "BidId",
                table: "ParticipationRequestLines");
        }
    }
}
