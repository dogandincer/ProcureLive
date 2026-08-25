using FluentValidation;
using ProcureLive.Application.Common.Security;

namespace ProcureLive.Application.Users.Commands.AssignRoles;

public sealed class AssignRolesCommandValidator : AbstractValidator<AssignRolesCommand>
{
    private static readonly string[] ValidRoles = { Roles.Admin, Roles.Buyer, Roles.Supplier };

    public AssignRolesCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Roles).NotEmpty();
        RuleForEach(x => x.Roles).Must(role => ValidRoles.Contains(role))
            .WithMessage($"Rol şunlardan biri olmalıdır: {string.Join(", ", ValidRoles)}");
    }
}
