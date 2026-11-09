namespace ProcureLive.Domain.Enums;

/// <summary>ERP entegrasyonunun çalışma modu. Simulation: ERP'den okunur, yazılacak XML'ler hazırlanıp kayda geçer ama gönderilmez.</summary>
public enum ErpMode
{
    Off = 0,
    Simulation = 1,
    Live = 2
}

public enum ErpSyncKind
{
    EntityExport = 0,
    ItemExport = 1,
    OfferCreate = 2,
    OfferRevise = 3,
    OrderCreate = 4
}

public enum ErpSyncStatus
{
    Pending = 0,

    /// <summary>Simülasyon modunda hazırlandı, ERP'ye gönderilmedi.</summary>
    Simulated = 1,

    Done = 2,
    Failed = 3,

    /// <summary>Bilerek yapılmadı (ör. iş sırasında mod kapatıldı, gerekli eşleme yok).</summary>
    Skipped = 4
}
