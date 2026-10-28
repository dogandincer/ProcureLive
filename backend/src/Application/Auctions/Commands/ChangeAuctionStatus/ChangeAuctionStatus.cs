using FluentValidation;
using MediatR;
using ProcureLive.Application.Auctions.Status;
using ProcureLive.Domain.Enums;
using ProcureLive.Application.Common.Security;

namespace ProcureLive.Application.Auctions.Commands.ChangeAuctionStatus;

[RequiresPermission(PermissionCodes.AuctionsManage)]
public sealed record ChangeAuctionStatusCommand(
    Guid AuctionId, AuctionStatus TargetStatus, string? Reason, DateTime? NewStartAt, DateTime? NewEndAt) : IRequest, IAuctionScopedRequest;

public sealed class ChangeAuctionStatusCommandValidator : AbstractValidator<ChangeAuctionStatusCommand>
{
    public ChangeAuctionStatusCommandValidator()
    {
        RuleFor(x => x.TargetStatus).IsInEnum();
        RuleFor(x => x.Reason).MaximumLength(500).WithMessage("Gerekçe en fazla 500 karakter olabilir.");
        RuleFor(x => x.Reason)
            .Must(r => !string.IsNullOrWhiteSpace(r))
            .When(x => AuctionStatusMachine.RequiresReason(x.TargetStatus))
            .WithMessage("Bu durum değişikliği için gerekçe zorunludur.");
        RuleFor(x => x.NewEndAt)
            .GreaterThan(x => x.NewStartAt!.Value)
            .When(x => x.NewStartAt.HasValue && x.NewEndAt.HasValue)
            .WithMessage("Bitiş tarihi başlangıçtan sonra olmalıdır.");
    }
}

public sealed class ChangeAuctionStatusCommandHandler : IRequestHandler<ChangeAuctionStatusCommand>
{
    private readonly AuctionStatusService _statusService;

    public ChangeAuctionStatusCommandHandler(AuctionStatusService statusService)
    {
        _statusService = statusService;
    }

    public Task Handle(ChangeAuctionStatusCommand request, CancellationToken cancellationToken) =>
        _statusService.ChangeAsync(
            request.AuctionId, request.TargetStatus, request.Reason, request.NewStartAt, request.NewEndAt, cancellationToken);
}
