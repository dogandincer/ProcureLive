using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Domain.Entities.Companies;

namespace ProcureLive.Application.Companies.Commands.AddContactPerson;

public sealed class AddContactPersonCommandHandler : IRequestHandler<AddContactPersonCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public AddContactPersonCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(AddContactPersonCommand request, CancellationToken cancellationToken)
    {
        bool companyExists = await _context.Companies.AnyAsync(c => c.Id == request.CompanyId, cancellationToken);

        if (!companyExists)
        {
            throw new NotFoundException(nameof(Company), request.CompanyId);
        }

        var contactPerson = new ContactPerson
        {
            CompanyId = request.CompanyId,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Title = request.Title,
            Email = request.Email,
            Phone = request.Phone,
            IsPrimary = request.IsPrimary
        };

        _context.ContactPersons.Add(contactPerson);
        await _context.SaveChangesAsync(cancellationToken);

        return contactPerson.Id;
    }
}
