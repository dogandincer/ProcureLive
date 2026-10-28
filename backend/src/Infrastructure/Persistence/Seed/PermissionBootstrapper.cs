using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProcureLive.Application.Common.Security;
using ProcureLive.Domain.Entities.Identity;
using ProcureLive.Infrastructure.Identity;

namespace ProcureLive.Infrastructure.Persistence.Seed;

/// <summary>
/// İdempotent: <see cref="PermissionCodes"/> kataloğunu Permissions tablosuna yazar. Yeni eklenen bir yetki
/// Satınalmacı rolüne varsayılan olarak verilir (mevcut davranış korunur); sonradan Admin'in kaldırdığı yetki geri eklenmez.
/// </summary>
public static class PermissionBootstrapper
{
    public static async Task RunAsync(IServiceProvider services)
    {
        ApplicationDbContext db = services.GetRequiredService<ApplicationDbContext>();
        RoleManager<ApplicationRole> roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
        ApplicationRole? buyer = await roleManager.FindByNameAsync(Roles.Buyer);

        Dictionary<string, Permission> existing = await db.Permissions.ToDictionaryAsync(p => p.Code);
        foreach ((string code, string description) in PermissionCodes.All)
        {
            if (existing.TryGetValue(code, out Permission? permission))
            {
                permission.Description = description;
                continue;
            }

            permission = new Permission { Code = code, Description = description };
            db.Permissions.Add(permission);
            if (buyer is not null)
            {
                db.RolePermissions.Add(new RolePermission { RoleId = buyer.Id, Permission = permission });
            }
        }

        await db.SaveChangesAsync();
    }
}
