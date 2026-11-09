using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Domain.Common;
using ProcureLive.Domain.Entities.Audit;
using ProcureLive.Domain.Entities.Notifications;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Stamps CreatedAt/CreatedBy/UpdatedAt/UpdatedBy on AuditableEntity and writes an AuditLog row
/// for every tracked Added/Modified/Deleted entity, in the same SaveChanges transaction.
/// </summary>
public sealed class AuditableEntitySaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTime;

    public AuditableEntitySaveChangesInterceptor(ICurrentUserService currentUserService, IDateTime dateTime)
    {
        _currentUserService = currentUserService;
        _dateTime = dateTime;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        ProcessAuditableEntities(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ProcessAuditableEntities(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ProcessAuditableEntities(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        DateTime utcNow = _dateTime.UtcNow;
        Guid? userId = _currentUserService.UserId;
        string? userName = _currentUserService.UserName;

        var auditEntries = new List<AuditLog>();

        foreach (EntityEntry entry in context.ChangeTracker.Entries())
        {
            if (entry.Entity is AuditableEntity auditable)
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        auditable.CreatedAt = utcNow;
                        auditable.CreatedBy = userName;
                        break;
                    case EntityState.Modified:
                        auditable.UpdatedAt = utcNow;
                        auditable.UpdatedBy = userName;
                        break;
                }
            }

            // ERP iş kuyruğu ve teklif bağlantıları teknik kayıttır; her durum değişikliği denetim kaydını doldurmasın.
            if (entry.Entity is AuditLog or Domain.Entities.Erp.ErpSyncJob or Domain.Entities.Erp.ErpOfferLink
                || entry.State is EntityState.Detached or EntityState.Unchanged)
            {
                continue;
            }

            AuditLog? logEntry = BuildAuditLog(entry, utcNow, userId, userName);

            if (logEntry is not null)
            {
                auditEntries.Add(logEntry);
            }
        }

        foreach (AuditLog auditEntry in auditEntries)
        {
            context.Set<AuditLog>().Add(auditEntry);
        }
    }

    /// <summary>Denetim kaydına değeri yazılmayan alanlar (şifreli de olsa sır taşıyanlar); yalnızca değiştiği görünür.</summary>
    private static readonly HashSet<string> MaskedProperties = [nameof(EmailSettings.SmtpPasswordProtected), nameof(Domain.Entities.Erp.ErpSettings.PasswordProtected)];

    private static object? Mask(bool masked, object? value) => masked && value is not null ? "***" : value;

    private static AuditLog? BuildAuditLog(EntityEntry entry, DateTime utcNow, Guid? userId, string? userName)
    {
        if (entry.Entity is not BaseEntity baseEntity)
        {
            return null;
        }

        AuditAction? action = entry.State switch
        {
            EntityState.Added => AuditAction.Create,
            EntityState.Deleted => AuditAction.Delete,
            EntityState.Modified => AuditAction.Update,
            _ => null
        };

        if (action is null)
        {
            return null;
        }

        var oldValues = new Dictionary<string, object?>();
        var newValues = new Dictionary<string, object?>();
        var affectedColumns = new List<string>();

        foreach (PropertyEntry property in entry.Properties)
        {
            string propertyName = property.Metadata.Name;
            bool masked = MaskedProperties.Contains(propertyName);

            switch (entry.State)
            {
                case EntityState.Added:
                    newValues[propertyName] = Mask(masked, property.CurrentValue);
                    break;
                case EntityState.Deleted:
                    oldValues[propertyName] = Mask(masked, property.OriginalValue);
                    break;
                case EntityState.Modified when property.IsModified:
                    oldValues[propertyName] = Mask(masked, property.OriginalValue);
                    newValues[propertyName] = Mask(masked, property.CurrentValue);
                    affectedColumns.Add(propertyName);
                    break;
            }
        }

        return new AuditLog
        {
            EntityName = entry.Entity.GetType().Name,
            EntityId = baseEntity.Id,
            Action = action.Value,
            ChangedByUserId = userId,
            ChangedByUserName = userName,
            ChangedAt = utcNow,
            OldValues = oldValues.Count > 0 ? JsonSerializer.Serialize(oldValues) : null,
            NewValues = newValues.Count > 0 ? JsonSerializer.Serialize(newValues) : null,
            AffectedColumns = affectedColumns.Count > 0 ? JsonSerializer.Serialize(affectedColumns) : null
        };
    }
}
