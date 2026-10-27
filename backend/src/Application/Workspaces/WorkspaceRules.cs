using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Domain.Entities.Workspaces;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Workspaces;

/// <summary>İşyeri ortak kuralları: bağlı firma zorunlu ve geçerli, ad firma içinde benzersiz, firma başına tek varsayılan.</summary>
internal static class WorkspaceRules
{
    public static void AddFirmAndNameRules<T>(
        AbstractValidator<T> validator,
        IApplicationDbContext context,
        Func<T, Guid?> id,
        System.Linq.Expressions.Expression<Func<T, Guid>> firmIdExpression,
        System.Linq.Expressions.Expression<Func<T, string>> name)
    {
        Func<T, Guid> firmId = firmIdExpression.Compile();

        validator.RuleFor(firmIdExpression)
            .NotEmpty().WithMessage("Bağlı olduğu firma seçilmelidir.")
            .MustAsync(async (fid, ct) => await context.Companies.AnyAsync(c => c.Id == fid && c.CompanyType == CompanyType.Internal, ct))
            .WithMessage("Seçilen firma bulunamadı.");

        validator.RuleFor(name)
            .NotEmpty()
            .MaximumLength(150)
            .MustAsync(async (cmd, n, ct) =>
            {
                Guid fid = firmId(cmd);
                Guid? self = id(cmd);
                return !await context.Workspaces.AnyAsync(w => w.FirmId == fid && w.Name == n && w.Id != self, ct);
            })
            .WithMessage("Bu firmada aynı isimde bir işyeri zaten mevcut.");
    }

    /// <summary>Firmanın ilk işyeri kendiliğinden varsayılan; varsayılan işaretlenirse firmanın diğerlerinden kaldırılır.</summary>
    public static async Task ApplyDefaultAsync(IApplicationDbContext context, Workspace workspace, bool requested, CancellationToken ct)
    {
        List<Workspace> siblings = await context.Workspaces
            .Where(w => w.FirmId == workspace.FirmId && w.Id != workspace.Id)
            .ToListAsync(ct);

        bool otherDefault = siblings.Any(w => w.IsDefault);
        bool makeDefault = requested || !otherDefault;

        if (makeDefault)
        {
            siblings.Where(w => w.IsDefault).ToList().ForEach(w => w.IsDefault = false);
        }

        workspace.IsDefault = makeDefault;
    }
}
