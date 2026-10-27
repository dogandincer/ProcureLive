using MediatR;
using ProcureLive.Application.Common.Security;

namespace ProcureLive.Application.Companies.Commands.EvaluateCompany;

public sealed record EvaluateCompanyCommand(
    Guid CompanyId,
    int QualityScore,
    int DeliveryScore,
    int CommunicationScore,
    int PriceScore,
    int ComplianceScore,
    string? Comments,
    string? SourceType,
    Guid? SourceProcessId) : IRequest<Guid>, ICompanyScopedRequest;
