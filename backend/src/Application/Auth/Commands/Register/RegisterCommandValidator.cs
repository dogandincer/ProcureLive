using FluentValidation;
using ProcureLive.Application.Common.Security;

namespace ProcureLive.Application.Auth.Commands.Register;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    private static readonly string[] ValidRoles = { Roles.Admin, Roles.Buyer, Roles.Supplier };

    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(250);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Role).NotEmpty().Must(role => ValidRoles.Contains(role))
            .WithMessage($"Rol şunlardan biri olmalıdır: {string.Join(", ", ValidRoles)}");
        RuleFor(x => x.CompanyId)
            .NotEmpty()
            .When(x => x.Role == Roles.Supplier)
            .WithMessage("Supplier rolündeki kullanıcılar bir Cari'ye bağlı olmalıdır.");
    }
}
