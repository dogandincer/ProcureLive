using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcureLive.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFirmsAndWorkspaceFirm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Workspaces_Name",
                table: "Workspaces");

            migrationBuilder.DropIndex(
                name: "IX_Companies_TaxNumber",
                table: "Companies");

            migrationBuilder.AddColumn<Guid>(
                name: "FirmId",
                table: "Workspaces",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "Workspaces",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "AppTitleSuffix",
                table: "Companies",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Companies",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email2",
                table: "Companies",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Fax",
                table: "Companies",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDefaultFirm",
                table: "Companies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MobilePhone",
                table: "Companies",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Phone2",
                table: "Companies",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShortName",
                table: "Companies",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Workspaces_FirmId_Name",
                table: "Workspaces",
                columns: new[] { "FirmId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Companies_Code",
                table: "Companies",
                column: "Code",
                unique: true,
                filter: "\"Code\" IS NOT NULL AND NOT \"IsDeleted\"");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_TaxNumber",
                table: "Companies",
                column: "TaxNumber",
                unique: true,
                filter: "\"TaxNumber\" <> '' AND NOT \"IsDeleted\"");

            migrationBuilder.AddForeignKey(
                name: "FK_Workspaces_Companies_FirmId",
                table: "Workspaces",
                column: "FirmId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Workspaces_Companies_FirmId",
                table: "Workspaces");

            migrationBuilder.DropIndex(
                name: "IX_Workspaces_FirmId_Name",
                table: "Workspaces");

            migrationBuilder.DropIndex(
                name: "IX_Companies_Code",
                table: "Companies");

            migrationBuilder.DropIndex(
                name: "IX_Companies_TaxNumber",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "FirmId",
                table: "Workspaces");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "Workspaces");

            migrationBuilder.DropColumn(
                name: "AppTitleSuffix",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "Email2",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "Fax",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "IsDefaultFirm",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "MobilePhone",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "Phone2",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "ShortName",
                table: "Companies");

            migrationBuilder.CreateIndex(
                name: "IX_Workspaces_Name",
                table: "Workspaces",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Companies_TaxNumber",
                table: "Companies",
                column: "TaxNumber",
                unique: true);
        }
    }
}
