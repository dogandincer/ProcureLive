using MediatR;
using ProcureLive.Domain.Enums;
using ProcureLive.Application.Common.Security;

namespace ProcureLive.Application.Companies.Commands.CreateCompany;

/// <summary>
/// Tedarikçi cari oluşturur (Firma tanımı Firma Tanım ekranından yapılır). Kod boşsa CARI-#### üretilir;
/// isteğe bağlı olarak sektörler, ek görünür işyerleri ve tedarikçinin giriş kullanıcısı aynı adımda açılır.
/// </summary>
[RequiresPermission(PermissionCodes.CompaniesManage)]
public sealed record CreateCompanyCommand(
    string Name,
    string TaxNumber,
    string TaxOffice,
    CompanyType CompanyType,
    string? Website,
    Guid? WorkspaceId,
    string? TradeName = null,
    string? MersisNo = null,
    string? Phone = null,
    string? Email = null,
    int? FoundationYear = null,
    int? EmployeeCount = null,
    string? Description = null,
    string? Code = null,
    IReadOnlyList<Guid>? VisibleWorkspaceIds = null,
    IReadOnlyList<Guid>? SectorIds = null,
    string? UserEmail = null,
    string? UserPassword = null,
    string? UserFirstName = null,
    string? UserLastName = null) : IRequest<Guid>, IWorkspaceTargetRequest;
