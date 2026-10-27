using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcureLive.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddParticipationRequestsAndCariWorkspaces : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ResponsibleUserId",
                table: "Auctions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WorkspaceId",
                table: "Auctions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CompanyWorkspaces",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyWorkspaces", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyWorkspaces_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CompanyWorkspaces_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ParticipationRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AuctionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CompanyApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompanyApprovedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    DecidedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DecidedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RejectReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParticipationRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParticipationRequests_Auctions_AuctionId",
                        column: x => x.AuctionId,
                        principalTable: "Auctions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ParticipationRequests_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ParticipationRequestLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ParticipationRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuctionItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParticipationRequestLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParticipationRequestLines_AuctionItems_AuctionItemId",
                        column: x => x.AuctionItemId,
                        principalTable: "AuctionItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ParticipationRequestLines_ParticipationRequests_Participati~",
                        column: x => x.ParticipationRequestId,
                        principalTable: "ParticipationRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Auctions_WorkspaceId",
                table: "Auctions",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyWorkspaces_CompanyId_WorkspaceId",
                table: "CompanyWorkspaces",
                columns: new[] { "CompanyId", "WorkspaceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompanyWorkspaces_WorkspaceId",
                table: "CompanyWorkspaces",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_ParticipationRequestLines_AuctionItemId",
                table: "ParticipationRequestLines",
                column: "AuctionItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ParticipationRequestLines_ParticipationRequestId_AuctionIte~",
                table: "ParticipationRequestLines",
                columns: new[] { "ParticipationRequestId", "AuctionItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ParticipationRequests_AuctionId_CompanyId",
                table: "ParticipationRequests",
                columns: new[] { "AuctionId", "CompanyId" });

            migrationBuilder.CreateIndex(
                name: "IX_ParticipationRequests_CompanyId",
                table: "ParticipationRequests",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_ParticipationRequests_Status",
                table: "ParticipationRequests",
                column: "Status");

            migrationBuilder.AddForeignKey(
                name: "FK_Auctions_Workspaces_WorkspaceId",
                table: "Auctions",
                column: "WorkspaceId",
                principalTable: "Workspaces",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // Mevcut ihaleler: işyeri = sahip firmanın varsayılan (yoksa ilk) aktif işyeri;
            // sorumlu satınalmacı = ihaleyi oluşturan kullanıcı (CreatedBy kullanıcı adıdır).
            migrationBuilder.Sql("""
                UPDATE "Auctions" a SET "WorkspaceId" = (
                    SELECT w."Id" FROM "Workspaces" w
                    WHERE w."FirmId" = a."OwnerCompanyId" AND w."IsActive"
                    ORDER BY w."IsDefault" DESC, w."CreatedAt"
                    LIMIT 1)
                WHERE a."WorkspaceId" IS NULL;

                UPDATE "Auctions" a SET "ResponsibleUserId" = u."Id"
                FROM "AspNetUsers" u
                WHERE a."ResponsibleUserId" IS NULL AND a."CreatedBy" IS NOT NULL
                  AND (u."UserName" = a."CreatedBy" OR u."Email" = a."CreatedBy");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Auctions_Workspaces_WorkspaceId",
                table: "Auctions");

            migrationBuilder.DropTable(
                name: "CompanyWorkspaces");

            migrationBuilder.DropTable(
                name: "ParticipationRequestLines");

            migrationBuilder.DropTable(
                name: "ParticipationRequests");

            migrationBuilder.DropIndex(
                name: "IX_Auctions_WorkspaceId",
                table: "Auctions");

            migrationBuilder.DropColumn(
                name: "ResponsibleUserId",
                table: "Auctions");

            migrationBuilder.DropColumn(
                name: "WorkspaceId",
                table: "Auctions");
        }
    }
}
