using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Domain.Entities.Companies;

namespace ProcureLive.Application.Firms.Commands;

/// <summary>Firma silme: en az bir firma kalmalı; varsayılan, işyeri bağlı veya ihale/RFI/RFQ sahibi firma silinemez.</summary>
public sealed record DeleteFirmCommand(Guid Id) : IRequest;

public sealed class DeleteFirmCommandHandler : IRequestHandler<DeleteFirmCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTime _dateTime;

    public DeleteFirmCommandHandler(IApplicationDbContext context, IDateTime dateTime)
    {
        _context = context;
        _dateTime = dateTime;
    }

    public async Task Handle(DeleteFirmCommand request, CancellationToken cancellationToken)
    {
        Company firm = await FirmRules.Firms(_context).FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Firm", request.Id);

        if (await FirmRules.Firms(_context).CountAsync(cancellationToken) <= 1)
            throw new ConflictException("En az bir firma tanımlı olmalıdır.");
        if (firm.IsDefaultFirm)
            throw new ConflictException("Varsayılan firma silinemez; önce başka bir firmayı varsayılan yapın.");
        if (await _context.Workspaces.AnyAsync(w => w.FirmId == firm.Id, cancellationToken))
            throw new ConflictException("İşyeri bağlı olan firma silinemez. Önce işyerlerini başka bir firmaya taşıyın.");
        if (await _context.Auctions.AnyAsync(a => a.OwnerCompanyId == firm.Id, cancellationToken)
            || await _context.RfiProcesses.AnyAsync(p => p.OwnerCompanyId == firm.Id, cancellationToken)
            || await _context.RfqProcesses.AnyAsync(p => p.OwnerCompanyId == firm.Id, cancellationToken))
            throw new ConflictException("İhale, RFI veya RFQ sahibi olan firma silinemez.");

        firm.IsDeleted = true;
        firm.DeletedAt = _dateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
    }
}

public sealed record SetDefaultFirmCommand(Guid Id) : IRequest;

public sealed class SetDefaultFirmCommandHandler : IRequestHandler<SetDefaultFirmCommand>
{
    private readonly IApplicationDbContext _context;

    public SetDefaultFirmCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(SetDefaultFirmCommand request, CancellationToken cancellationToken)
    {
        List<Company> firms = await FirmRules.Firms(_context).ToListAsync(cancellationToken);
        if (firms.All(f => f.Id != request.Id))
            throw new NotFoundException("Firm", request.Id);

        firms.ForEach(f => f.IsDefaultFirm = f.Id == request.Id);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>
/// Logo yükleme: yalnızca PNG / JPEG / WEBP / ICO (içerik imzasıyla doğrulanır — uzantıya/Content-Type'a güvenilmez;
/// SVG kabul edilmez çünkü betik içerebilir). Dosya wwwroot/uploads/logos altına yazılır, eski logo silinir.
/// </summary>
public sealed record UploadFirmLogoCommand(Guid FirmId, byte[] Content) : IRequest<string>;

public sealed class UploadFirmLogoCommandHandler : IRequestHandler<UploadFirmLogoCommand, string>
{
    public const int MaxBytes = 2 * 1024 * 1024;

    private readonly IApplicationDbContext _context;
    private readonly IFileStorage _storage;
    private readonly IDateTime _dateTime;

    public UploadFirmLogoCommandHandler(IApplicationDbContext context, IFileStorage storage, IDateTime dateTime)
    {
        _context = context;
        _storage = storage;
        _dateTime = dateTime;
    }

    public async Task<string> Handle(UploadFirmLogoCommand request, CancellationToken cancellationToken)
    {
        Company firm = await FirmRules.Firms(_context).FirstOrDefaultAsync(c => c.Id == request.FirmId, cancellationToken)
            ?? throw new NotFoundException("Firm", request.FirmId);

        if (request.Content.Length == 0 || request.Content.Length > MaxBytes)
            throw new ConflictException("Logo dosyası en fazla 2 MB olabilir.");

        string extension = DetectImageExtension(request.Content)
            ?? throw new ConflictException("Logo PNG, JPG, WEBP veya ICO formatında olmalıdır.");

        string fileName = $"{firm.Id:N}-{_dateTime.UtcNow:yyyyMMddHHmmss}.{extension}";
        await using var stream = new MemoryStream(request.Content);
        string url = await _storage.SaveAsync("logos", fileName, stream, cancellationToken);

        string? previous = firm.LogoUrl;
        firm.LogoUrl = url;
        await _context.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrEmpty(previous))
            await _storage.DeleteAsync(previous, cancellationToken);

        return url;
    }

    private static string? DetectImageExtension(byte[] b)
    {
        if (b.Length >= 8 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47) return "png";
        if (b.Length >= 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF) return "jpg";
        if (b.Length >= 12 && b[0] == 'R' && b[1] == 'I' && b[2] == 'F' && b[3] == 'F' && b[8] == 'W' && b[9] == 'E' && b[10] == 'B' && b[11] == 'P') return "webp";
        if (b.Length >= 4 && b[0] == 0x00 && b[1] == 0x00 && b[2] == 0x01 && b[3] == 0x00) return "ico";
        return null;
    }
}

public sealed record RemoveFirmLogoCommand(Guid FirmId) : IRequest;

public sealed class RemoveFirmLogoCommandHandler : IRequestHandler<RemoveFirmLogoCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IFileStorage _storage;

    public RemoveFirmLogoCommandHandler(IApplicationDbContext context, IFileStorage storage)
    {
        _context = context;
        _storage = storage;
    }

    public async Task Handle(RemoveFirmLogoCommand request, CancellationToken cancellationToken)
    {
        Company firm = await FirmRules.Firms(_context).FirstOrDefaultAsync(c => c.Id == request.FirmId, cancellationToken)
            ?? throw new NotFoundException("Firm", request.FirmId);

        string? previous = firm.LogoUrl;
        firm.LogoUrl = null;
        await _context.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrEmpty(previous))
            await _storage.DeleteAsync(previous, cancellationToken);
    }
}
