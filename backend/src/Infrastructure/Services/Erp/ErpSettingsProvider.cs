using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Domain.Entities.Erp;

namespace ProcureLive.Infrastructure.Services.Erp;

/// <summary>Premium özellik anahtarları sunucu yapılandırmasından okunur (Features:ErpIntegration); lisanslama kurulumda yapılır.</summary>
public sealed class ConfigurationFeatureFlags : IFeatureFlags
{
    public ConfigurationFeatureFlags(IConfiguration configuration)
    {
        ErpIntegration = configuration.GetValue("Features:ErpIntegration", false);
    }

    public bool ErpIntegration { get; }
}

/// <summary>Kayıtlı ERP ayarını şifresi çözülmüş halde 30 sn önbellekler (singleton); kayıt ekranı <see cref="Invalidate"/> çağırır.</summary>
public sealed class ErpSettingsProvider : IErpSettingsProvider
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ISecretProtector _protector;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private (ErpConnection Value, DateTime LoadedAt)? _cached;

    public ErpSettingsProvider(IServiceScopeFactory scopeFactory, ISecretProtector protector)
    {
        _scopeFactory = scopeFactory;
        _protector = protector;
    }

    public async Task<ErpConnection> GetAsync(CancellationToken cancellationToken)
    {
        if (_cached is { } hit && DateTime.UtcNow - hit.LoadedAt < CacheDuration) return hit.Value;

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_cached is { } again && DateTime.UtcNow - again.LoadedAt < CacheDuration) return again.Value;

            using IServiceScope scope = _scopeFactory.CreateScope();
            IApplicationDbContext context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            ErpSettings? s = await context.ErpSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);

            ErpConnection value = s is null
                ? ErpConnection.Off
                : new ErpConnection(s.Mode, s.GetServiceUrl, s.SaveServiceUrl, s.Username,
                    s.PasswordProtected is null ? null : _protector.Unprotect(s.PasswordProtected),
                    s.CoCode, s.BranchCode, s.OfferDocTraCode, s.OrderDocTraCode, s.DocNoPrefix, s.EntityCodePrefix,
                    s.ExportNewSuppliers, s.ExportNewProducts);

            _cached = (value, DateTime.UtcNow);
            return value;
        }
        finally
        {
            _lock.Release();
        }
    }

    public void Invalidate() => _cached = null;
}
