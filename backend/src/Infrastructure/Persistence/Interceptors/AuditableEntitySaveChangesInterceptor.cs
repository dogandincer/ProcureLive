using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Domain.Common;
using ProcureLive.Domain.Entities.Audit;
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

            if (entry.Entity is AuditLog || entry.State is EntityState.Detached or EntityState.Unchanged)
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

            switch (entry.State)
            {
                case EntityState.Added:
                    newValues[propertyName] = property.CurrentValue;
                    break;
                case EntityState.Deleted:
                    oldValues[propertyName] = property.OriginalValue;
                    break;
                case EntityState.Modified when property.IsModified:
                    oldValues[propertyName] = property.OriginalValue;
                    newValues[propertyName] = property.CurrentValue;
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
