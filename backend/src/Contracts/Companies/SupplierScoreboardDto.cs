namespace ProcureLive.Contracts.Companies;

/// <summary>Cari değerlendirme panosu: sıralanmış cariler, en popüler sektörler, sektör bazlı en iyi cariler.</summary>
public sealed record SupplierScoreboardDto(
    IReadOnlyCollection<SupplierScoreDto> TopSuppliers,
    IReadOnlyCollection<SectorPopularityDto> PopularSectors,
    IReadOnlyCollection<TopSupplierPerSectorDto> TopSupplierPerSector,
    int TotalEvaluatedSuppliers,
    int TotalEvaluations);

public sealed record SupplierScoreDto(
    Guid CompanyId,
    string CompanyName,
    decimal AverageScore,
    int EvaluationCount,
    decimal AvgQuality,
    decimal AvgDelivery,
    decimal AvgCommunication,
    decimal AvgPrice,
    decimal AvgCompliance,
    IReadOnlyCollection<string> Sectors);

public sealed record SectorPopularityDto(
    Guid SectorId,
    string SectorName,
    int CompanyCount,
    int EvaluatedCompanyCount,
    decimal? AverageScore);

public sealed record TopSupplierPerSectorDto(
    Guid SectorId,
    string SectorName,
    Guid? CompanyId,
    string? CompanyName,
    decimal? AverageScore,
    int EvaluationCount);
