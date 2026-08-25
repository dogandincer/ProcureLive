using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProcureLive.Application.Common.Security;
using ProcureLive.Infrastructure.Identity;

namespace ProcureLive.Infrastructure.Persistence.Seed;

/// <summary>
/// Idempotent startup seeder for the very first Admin user (roles/currencies/category come from
/// the SeedReferenceData migration; a user needs UserManager for correct password hashing, so it
/// can't be seeded via raw migration InsertData).
/// </summary>
public static class InitialAdminSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        UserManager<ApplicationUser> userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        IConfiguration configuration = services.GetRequiredService<IConfiguration>();
        ILogger logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("InitialAdminSeeder");

        string email = configuration["InitialAdmin:Email"] ?? "admin@procurelive.local";
        string password = configuration["InitialAdmin:Password"] ?? "ChangeMe123!";

        ApplicationUser? existing = await userManager.FindByEmailAsync(email);

        if (existing is not null)
        {
            return;
        }

        var admin = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = "System",
            LastName = "Admin",
            IsActive = true
        };

        IdentityResult createResult = await userManager.CreateAsync(admin, password);

        if (!createResult.Succeeded)
        {
            logger.LogError(
                "İlk Admin kullanıcısı oluşturulamadı: {Errors}",
                string.Join("; ", createResult.Errors.Select(e => e.Description)));
            return;
        }

        await userManager.AddToRoleAsync(admin, Roles.Admin);
        logger.LogInformation("İlk Admin kullanıcısı oluşturuldu: {Email}", email);
    }
}
