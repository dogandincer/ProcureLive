using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Contracts.Companies;
using ProcureLive.Domain.Entities.Companies;

namespace ProcureLive.Application.Companies.Queries.GetCompanyById;

public sealed class GetCompanyByIdQueryHandler : IRequestHandler<GetCompanyByIdQuery, CompanyDetailDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetCompanyByIdQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<CompanyDetailDto> Handle(GetCompanyByIdQuery request, CancellationToken cancellationToken)
    {
        CompanyDetailDto? dto = await _context.Companies
            .Where(c => c.Id == request.Id)
            .ProjectTo<CompanyDetailDto>(_mapper.ConfigurationProvider)
            .SingleOrDefaultAsync(cancellationToken);

        if (dto is null)
        {
            throw new NotFoundException(nameof(Company), request.Id);
        }

        return dto;
    }
}
