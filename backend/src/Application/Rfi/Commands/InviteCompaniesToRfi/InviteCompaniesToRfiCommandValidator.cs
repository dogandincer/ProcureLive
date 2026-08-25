using FluentValidation;

namespace ProcureLive.Application.Rfi.Commands.InviteCompaniesToRfi;

public sealed class InviteCompaniesToRfiCommandValidator : AbstractValidator<InviteCompaniesToRfiCommand>
{
    public InviteCompaniesToRfiCommandValidator()
    {
        RuleFor(x => x.RfiProcessId).NotEmpty();
        RuleFor(x => x.CompanyIds).NotEmpty();
    }
}
