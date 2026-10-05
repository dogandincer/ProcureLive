using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcureLive.Application.Common.Security;
using ProcureLive.Application.Companies.Commands.AddAddress;
using ProcureLive.Application.Companies.Commands.AddContactPerson;
using ProcureLive.Application.Companies.Commands.AssignSectors;
using ProcureLive.Application.Companies.Commands.CreateCompany;
using ProcureLive.Application.Companies.Commands.DeleteCompany;
using ProcureLive.Application.Companies.Commands.UpdateCompany;
using ProcureLive.Application.Companies.Queries.GetCompaniesList;
using ProcureLive.Application.Companies.Queries.GetCompanyById;
using ProcureLive.Contracts.Common;
using ProcureLive.Contracts.Companies;
using ProcureLive.Domain.Enums;

namespace ProcureLive.WebApi.Controllers;

[ApiController]
[Route("api/companies")]
[Authorize]
public sealed class CompaniesController : ControllerBase
{
    private readonly ISender _sender;

    public CompaniesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [Authorize(Policy = Policies.RequireInternalUser)]
    public async Task<ActionResult<PagedResult<CompanyDto>>> GetList(
        [FromQuery] string? searchTerm,
        [FromQuery] string? companyType,
        [FromQuery] string? status,
        [FromQuery] Guid? sectorId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        PagedResult<CompanyDto> result = await _sender.Send(
            new GetCompaniesListQuery(searchTerm, companyType, status, sectorId, page, pageSize), cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CompanyDetailDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        CompanyDetailDto result = await _sender.Send(new GetCompanyByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = Policies.RequireInternalUser)]
    public async Task<ActionResult<Guid>> Create(CreateCompanyRequest request, CancellationToken cancellationToken)
    {
        CompanyType companyType;
        bool parsed = Enum.TryParse(request.CompanyType, true, out companyType);

        if (!parsed)
        {
            return BadRequest($"Geçersiz CompanyType: {request.CompanyType}");
        }

        Guid id = await _sender.Send(
            new CreateCompanyCommand(request.Name, request.TaxNumber, request.TaxOffice, companyType, request.Website),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateCompanyRequest request, CancellationToken cancellationToken)
    {
        CompanyStatus status;
        bool parsed = Enum.TryParse(request.Status, true, out status);

        if (!parsed)
        {
            return BadRequest($"Geçersiz Status: {request.Status}");
        }

        await _sender.Send(new UpdateCompanyCommand(id, request.Name, request.TaxOffice, status, request.Website), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.RequireInternalUser)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteCompanyCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/contacts")]
    public async Task<ActionResult<Guid>> AddContactPerson(Guid id, AddContactPersonRequest request, CancellationToken cancellationToken)
    {
        Guid contactId = await _sender.Send(
            new AddContactPersonCommand(id, request.FirstName, request.LastName, request.Title, request.Email, request.Phone, request.IsPrimary),
            cancellationToken);

        return Ok(contactId);
    }

    [HttpPut("{id:guid}/sectors")]
    [Authorize(Policy = Policies.RequireInternalUser)]
    public async Task<IActionResult> AssignSectors(Guid id, AssignSectorsRequest request, CancellationToken cancellationToken)
    {
        await _sender.Send(new AssignSectorsCommand(id, request.SectorIds), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/addresses")]
    public async Task<ActionResult<Guid>> AddAddress(Guid id, AddAddressRequest request, CancellationToken cancellationToken)
    {
        AddressType addressType;
        bool parsed = Enum.TryParse(request.AddressType, true, out addressType);

        if (!parsed)
        {
            return BadRequest($"Geçersiz AddressType: {request.AddressType}");
        }

        Guid addressId = await _sender.Send(
            new AddAddressCommand(id, addressType, request.Country, request.City, request.District, request.PostalCode, request.Line1, request.Line2),
            cancellationToken);

        return Ok(addressId);
    }
}
