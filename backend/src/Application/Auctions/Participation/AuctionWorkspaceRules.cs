using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Application.Common.Security;

namespace ProcureLive.Application.Auctions.Participation;

public static class AuctionWorkspaceRules
{
    /// <summary>
    /// İhalenin işyeri: istenen işyeri sahip firmaya ait olmalı; verilmemişse firmanın varsayılan (yoksa ilk) işyeri —
    /// satınalmacı için yalnızca yetkili olduğu işyerleri arasından. Firmanın hiç işyeri yoksa null
    /// (cari onayı firma düzeyinde değerlendirilir).
    /// </summary>
    public static async Task<Guid?> ResolveAsync(
        IApplicationDbContext context, Guid ownerFirmId, Guid? requested, DataScope scope, CancellationToken ct)
    {
        if (requested is Guid id)
        {
            bool belongs = await context.Workspaces.AnyAsync(w => w.Id == id && w.FirmId == ownerFirmId, ct);
            if (!belongs)
                throw new ConflictException("Seçilen işyeri ihale sahibi firmaya ait değil.");
            return id;
        }

        IQueryable<ProcureLive.Domain.Entities.Workspaces.Workspace> candidates =
            context.Workspaces.Where(w => w.FirmId == ownerFirmId && w.IsActive);
        if (!scope.IsUnrestricted)
        {
            List<Guid> allowed = scope.WorkspaceIds.ToList();
            candidates = candidates.Where(w => allowed.Contains(w.Id));
        }

        return await candidates
            .OrderByDescending(w => w.IsDefault).ThenBy(w => w.CreatedAt)
            .Select(w => (Guid?)w.Id)
            .FirstOrDefaultAsync(ct);
    }
}
