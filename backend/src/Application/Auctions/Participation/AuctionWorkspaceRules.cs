using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;

namespace ProcureLive.Application.Auctions.Participation;

public static class AuctionWorkspaceRules
{
    /// <summary>
    /// İhalenin işyeri: istenen işyeri sahip firmaya ait olmalı; verilmemişse firmanın varsayılan (yoksa ilk) işyeri.
    /// Firmanın hiç işyeri yoksa null (cari onayı firma düzeyinde değerlendirilir).
    /// </summary>
    public static async Task<Guid?> ResolveAsync(IApplicationDbContext context, Guid ownerFirmId, Guid? requested, CancellationToken ct)
    {
        if (requested is Guid id)
        {
            bool belongs = await context.Workspaces.AnyAsync(w => w.Id == id && w.FirmId == ownerFirmId, ct);
            if (!belongs)
                throw new ConflictException("Seçilen işyeri ihale sahibi firmaya ait değil.");
            return id;
        }

        return await context.Workspaces
            .Where(w => w.FirmId == ownerFirmId && w.IsActive)
            .OrderByDescending(w => w.IsDefault).ThenBy(w => w.CreatedAt)
            .Select(w => (Guid?)w.Id)
            .FirstOrDefaultAsync(ct);
    }
}
