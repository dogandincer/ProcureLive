using FluentValidation;

namespace ProcureLive.Application.Companies.Commands.AddContactPerson;

public sealed class AddContactPersonCommandValidator : AbstractValidator<AddContactPersonCommand>
{
    public AddContactPersonCommandValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(250);
    }
}
