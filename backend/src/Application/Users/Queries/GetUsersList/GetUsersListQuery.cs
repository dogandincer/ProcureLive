using MediatR;
using ProcureLive.Contracts.Common;
using ProcureLive.Contracts.Users;

namespace ProcureLive.Application.Users.Queries.GetUsersList;

/// <summary>Kind: "internal" (Admin/Satınalmacı — Kullanıcı Tanım), "supplier" (tedarikçi) veya null (hepsi).</summary>
public sealed record GetUsersListQuery(string? SearchTerm, Guid? CompanyId, int Page, int PageSize, string? Kind = null)
    : IRequest<PagedResult<UserDto>>;
