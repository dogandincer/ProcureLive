using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Domain.Entities.Companies;

namespace ProcureLive.Application.Companies.Commands.AddAddress;

public sealed class AddAddressCommandHandler : IRequestHandler<AddAddressCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public AddAddressCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(AddAddressCommand request, CancellationToken cancellationToken)
    {
        bool companyExists = await _context.Companies.AnyAsync(c => c.Id == request.CompanyId, cancellationToken);

        if (!companyExists)
        {
            throw new NotFoundException(nameof(Company), request.CompanyId);
        }

        var address = new Address
        {
            CompanyId = request.CompanyId,
            AddressType = request.AddressType,
            Country = request.Country,
            City = request.City,
            District = request.District,
            PostalCode = request.PostalCode,
            Line1 = request.Line1,
            Line2 = request.Line2
        };

        _context.Addresses.Add(address);
        await _context.SaveChangesAsync(cancellationToken);

        return address.Id;
    }
}
