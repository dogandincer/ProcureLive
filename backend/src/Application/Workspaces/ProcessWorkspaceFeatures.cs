using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Application.Common.Security;
using ProcureLive.Domain.Entities.Rfi;
using ProcureLive.Domain.Entities.Rfq;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Workspaces;

/// <summary>
/// RFI / RFQ sürecinin işyerini değiştirir (satınalmacı kapsamı bu işyerine göre uygulanır). Hedef işyeri sahip firmanın
/// aktif bir işyeri olmalı ve (satınalmacı için) yetkili olunmalı; kapanmış süreçte değiştirilemez.
/// </summary>
internal static class ProcessWorkspaceRules
{
    public const string ClosedMessage = "Kapanmış sürecin işyeri değiştirilemez.";

    public static bool IsFinal(ProcessStatus status) =>
        status is ProcessStatus.Closed or ProcessStatus.Evaluated or ProcessStatus.Awarded or ProcessStatus.Cancelled;

    public static async Task EnsureBelongsAsync(IApplicationDbContext context, Guid workspaceId, Guid ownerFirmId, CancellationToken ct)
    {
        if (!await context.Workspaces.AnyAsync(w => w.Id == workspaceId && w.FirmId == ownerFirmId && w.IsActive, ct))
            throw new ConflictException("Seçilen işyeri sahip firmaya ait değil.");
    }
}

public sealed record ChangeRfiWorkspaceCommand(Guid RfiProcessId, Guid TargetWorkspaceId)
    : IRequest, IRfiScopedRequest, IWorkspaceTargetRequest
{
    Guid? IWorkspaceTargetRequest.WorkspaceId => TargetWorkspaceId;
}

public sealed class ChangeRfiWorkspaceCommandHandler : IRequestHandler<ChangeRfiWorkspaceCommand>
{
    private readonly IApplicationDbContext _context;

    public ChangeRfiWorkspaceCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(ChangeRfiWorkspaceCommand request, CancellationToken cancellationToken)
    {
        RfiProcess process = await _context.RfiProcesses.FirstOrDefaultAsync(p => p.Id == request.RfiProcessId, cancellationToken)
            ?? throw new NotFoundException(nameof(RfiProcess), request.RfiProcessId);

        if (ProcessWorkspaceRules.IsFinal(process.Status))
            throw new ConflictException(ProcessWorkspaceRules.ClosedMessage);

        await ProcessWorkspaceRules.EnsureBelongsAsync(_context, request.TargetWorkspaceId, process.OwnerCompanyId, cancellationToken);
        process.WorkspaceId = request.TargetWorkspaceId;
        await _context.SaveChangesAsync(cancellationToken);
    }
}

public sealed record ChangeRfqWorkspaceCommand(Guid RfqProcessId, Guid TargetWorkspaceId)
    : IRequest, IRfqScopedRequest, IWorkspaceTargetRequest
{
    Guid? IWorkspaceTargetRequest.WorkspaceId => TargetWorkspaceId;
}

public sealed class ChangeRfqWorkspaceCommandHandler : IRequestHandler<ChangeRfqWorkspaceCommand>
{
    private readonly IApplicationDbContext _context;

    public ChangeRfqWorkspaceCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(ChangeRfqWorkspaceCommand request, CancellationToken cancellationToken)
    {
        RfqProcess process = await _context.RfqProcesses.FirstOrDefaultAsync(p => p.Id == request.RfqProcessId, cancellationToken)
            ?? throw new NotFoundException(nameof(RfqProcess), request.RfqProcessId);

        if (ProcessWorkspaceRules.IsFinal(process.Status))
            throw new ConflictException(ProcessWorkspaceRules.ClosedMessage);

        await ProcessWorkspaceRules.EnsureBelongsAsync(_context, request.TargetWorkspaceId, process.OwnerCompanyId, cancellationToken);
        process.WorkspaceId = request.TargetWorkspaceId;
        await _context.SaveChangesAsync(cancellationToken);
    }
}
