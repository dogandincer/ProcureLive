using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcureLive.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedReferenceData : Migration
    {
        private static readonly Guid AdminRoleId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid BuyerRoleId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        private static readonly Guid SupplierRoleId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        private static readonly Guid TryCurrencyId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        private static readonly Guid UsdCurrencyId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        private static readonly Guid EurCurrencyId = Guid.Parse("66666666-6666-6666-6666-666666666666");
        private static readonly Guid RootCategoryId = Guid.Parse("77777777-7777-7777-7777-777777777777");

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "Name", "NormalizedName", "ConcurrencyStamp" },
                values: new object[,]
                {
                    { AdminRoleId, "Admin", "ADMIN", "seed-role-admin" },
                    { BuyerRoleId, "Buyer", "BUYER", "seed-role-buyer" },
                    { SupplierRoleId, "Supplier", "SUPPLIER", "seed-role-supplier" }
                });

            migrationBuilder.InsertData(
                table: "Currencies",
                columns: new[] { "Id", "Code", "Name", "Symbol", "IsBaseCurrency", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { TryCurrencyId, "TRY", "Türk Lirası", "₺", true, DateTime.UtcNow, "seed", null, null },
                    { UsdCurrencyId, "USD", "US Dollar", "$", false, DateTime.UtcNow, "seed", null, null },
                    { EurCurrencyId, "EUR", "Euro", "€", false, DateTime.UtcNow, "seed", null, null }
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "Name", "Code", "ParentCategoryId", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { RootCategoryId, "Genel", "GENEL", null, DateTime.UtcNow, "seed", null, null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "Categories", keyColumn: "Id", keyValue: RootCategoryId);
            migrationBuilder.DeleteData(table: "Currencies", keyColumn: "Id", keyValue: EurCurrencyId);
            migrationBuilder.DeleteData(table: "Currencies", keyColumn: "Id", keyValue: UsdCurrencyId);
            migrationBuilder.DeleteData(table: "Currencies", keyColumn: "Id", keyValue: TryCurrencyId);
            migrationBuilder.DeleteData(table: "AspNetRoles", keyColumn: "Id", keyValue: SupplierRoleId);
            migrationBuilder.DeleteData(table: "AspNetRoles", keyColumn: "Id", keyValue: BuyerRoleId);
            migrationBuilder.DeleteData(table: "AspNetRoles", keyColumn: "Id", keyValue: AdminRoleId);
        }
    }
}
