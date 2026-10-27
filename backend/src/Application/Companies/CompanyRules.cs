using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Application.Common.Security;
using ProcureLive.Domain.Entities.Companies;

namespace ProcureLive.Application.Companies;

/// <summary>Tedarikçi cari kuralları: cari kodu üretimi/benzersizliği ve görünür işyerleri.</summary>
public static class CompanyRules
{
    public const string CodePrefix = "CARI-";

    public static async Task<string> NextCodeAsync(IApplicationDbContext context, CancellationToken ct)
    {
        HashSet<string> used = (await context.Companies.IgnoreQueryFilters()
            .Where(c => c.Code != null && c.Code.StartsWith(CodePrefix))
            .Select(c => c.Code!)
            .ToListAsync(ct)).ToHashSet();

        int n = 1;
        while (used.Contains($"{CodePrefix}{n:0000}")) n++;
        return $"{CodePrefix}{n:0000}";
    }

    /// <summary>Boşsa otomatik kod üretir; doluysa (silinmemiş kayıtlar arasında) benzersiz olmalı.</summary>
    public static async Task<string> ResolveCodeAsync(IApplicationDbContext context, string? code, Guid? exceptId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(code))
            return await NextCodeAsync(context, ct);

        string trimmed = code.Trim().ToUpperInvariant();
        bool taken = await context.Companies.AnyAsync(c => c.Code == trimmed && c.Id != exceptId, ct);
        if (taken)
            throw new ConflictException($"\"{trimmed}\" cari kodu başka bir kayıtta kullanılıyor.");
        return trimmed;
    }

    /// <summary>
    /// Carinin ek görünür işyerlerini verilen listeyle değiştirir (birincil işyeri listede olsa da ayrıca tutulmaz).
    /// Kapsamlı satınalmacı yalnızca kendi işyerlerini ekleyip çıkarabilir; kapsamı dışındaki bağlar korunur.
    /// </summary>
    public static async Task SetVisibleWorkspacesAsync(
        IApplicationDbContext context, Company company, IEnumerable<Guid> workspaceIds, DataScope scope, CancellationToken ct)
    {
        HashSet<Guid> wanted = workspaceIds.Where(id => id != company.WorkspaceId).ToHashSet();

        int existing = await context.Workspaces.CountAsync(w => wanted.Contains(w.Id), ct);
        if (existing != wanted.Count)
            throw new ConflictException("Seçilen işyerlerinden biri bulunamadı.");

        List<CompanyWorkspace> current = await context.CompanyWorkspaces.Where(x => x.CompanyId == company.Id).ToListAsync(ct);

        // Yalnızca yeni eklenenler kapsam kontrolünden geçer; mevcut (kapsam dışı) bağlar listede gelse de korunur.
        if (wanted.Any(id => current.All(l => l.WorkspaceId != id) && !scope.HasWorkspace(id)))
            throw new ForbiddenAccessException("Yetkili olmadığınız bir işyeri seçtiniz.");

        foreach (CompanyWorkspace link in current.Where(l => !wanted.Contains(l.WorkspaceId) && scope.HasWorkspace(l.WorkspaceId)))
            context.CompanyWorkspaces.Remove(link);

        foreach (Guid id in wanted.Where(id => current.All(l => l.WorkspaceId != id)))
            context.CompanyWorkspaces.Add(new CompanyWorkspace { CompanyId = company.Id, WorkspaceId = id });
    }

    /// <summary>Cari, işyerinde (birincil veya ek) görünür mü.</summary>
    public static Task<bool> IsVisibleInWorkspaceAsync(IApplicationDbContext context, Guid companyId, Guid workspaceId, CancellationToken ct) =>
        context.Companies.AnyAsync(c => c.Id == companyId
            && (c.WorkspaceId == workspaceId || c.VisibleWorkspaces.Any(v => v.WorkspaceId == workspaceId)), ct);
}
