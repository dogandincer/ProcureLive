using MediatR;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Application.Common.Security;
using ProcureLive.Contracts.Files;

namespace ProcureLive.Application.Files;

/// <summary>
/// Genel dosya yükleme: ihale ekleri, RFI dosya cevapları, ürün görselleri, cari belgeleri. Her amaç için izinli
/// uzantılar, azami boyut ve kimin yükleyebileceği sabittir; içerik imzası uzantıyla eşleşmeli (yeniden adlandırılmış
/// çalıştırılabilir dosya kabul edilmez). Dosya rastgele adla saklanır, özgün ad yalnızca gösterim için döner.
/// </summary>
public sealed record UploadFileCommand(string Purpose, string FileName, byte[] Content) : IRequest<UploadedFileDto>;

public sealed class UploadFileCommandHandler : IRequestHandler<UploadFileCommand, UploadedFileDto>
{
    private sealed record Rule(string[] Extensions, long MaxBytes, bool Internal, bool Supplier);

    private static readonly string[] Documents = [".pdf", ".doc", ".docx", ".xls", ".xlsx", ".png", ".jpg", ".jpeg", ".zip", ".txt", ".csv"];
    private static readonly string[] Images = [".png", ".jpg", ".jpeg", ".webp"];

    private static readonly Dictionary<string, Rule> Rules = new()
    {
        ["auction-attachment"] = new(Documents, 10 * 1024 * 1024, Internal: true, Supplier: false),
        ["rfi-answer"] = new(Documents, 10 * 1024 * 1024, Internal: false, Supplier: true),
        ["product-image"] = new(Images, 5 * 1024 * 1024, Internal: true, Supplier: false),
        ["company-document"] = new([".pdf", ".png", ".jpg", ".jpeg"], 10 * 1024 * 1024, Internal: true, Supplier: true),
    };

    public const long MaxBytes = 10 * 1024 * 1024;

    private static readonly Dictionary<string, string> ContentTypes = new()
    {
        [".pdf"] = "application/pdf",
        [".doc"] = "application/msword",
        [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        [".xls"] = "application/vnd.ms-excel",
        [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        [".png"] = "image/png",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".webp"] = "image/webp",
        [".zip"] = "application/zip",
        [".txt"] = "text/plain",
        [".csv"] = "text/csv",
    };

    private readonly IFileStorage _storage;
    private readonly ICurrentUserService _currentUser;

    public UploadFileCommandHandler(IFileStorage storage, ICurrentUserService currentUser)
    {
        _storage = storage;
        _currentUser = currentUser;
    }

    public async Task<UploadedFileDto> Handle(UploadFileCommand request, CancellationToken cancellationToken)
    {
        if (!Rules.TryGetValue(request.Purpose, out Rule? rule))
            throw new ConflictException("Bilinmeyen yükleme türü.");

        bool isInternal = _currentUser.IsInRole(Roles.Admin) || _currentUser.IsInRole(Roles.Buyer);
        if (isInternal ? !rule.Internal : !rule.Supplier)
            throw new ForbiddenAccessException("Bu tür dosyayı yükleme yetkiniz yok.");

        string ext = Path.GetExtension(request.FileName).ToLowerInvariant();
        if (!rule.Extensions.Contains(ext))
            throw new ConflictException($"Bu dosya türü desteklenmiyor. İzin verilenler: {string.Join(", ", rule.Extensions)}");
        if (request.Content.Length == 0)
            throw new ConflictException("Dosya boş.");
        if (request.Content.Length > rule.MaxBytes)
            throw new ConflictException($"Dosya en fazla {rule.MaxBytes / (1024 * 1024)} MB olabilir.");
        if (!SignatureMatches(ext, request.Content))
            throw new ConflictException("Dosya içeriği uzantısıyla uyuşmuyor.");

        string storedName = $"{Guid.NewGuid():N}{ext}";
        using var stream = new MemoryStream(request.Content);
        string url = await _storage.SaveAsync(request.Purpose, storedName, stream, cancellationToken);

        string displayName = Path.GetFileName(request.FileName);
        if (displayName.Length > 200) displayName = displayName[..190] + ext;
        return new UploadedFileDto(url, displayName, ContentTypes[ext], request.Content.LongLength);
    }

    private static bool StartsWith(byte[] data, params byte[] prefix) => data.Length >= prefix.Length && data.AsSpan(0, prefix.Length).SequenceEqual(prefix);

    private static bool SignatureMatches(string ext, byte[] data) => ext switch
    {
        ".pdf" => StartsWith(data, 0x25, 0x50, 0x44, 0x46),
        ".png" => StartsWith(data, 0x89, 0x50, 0x4E, 0x47),
        ".jpg" or ".jpeg" => StartsWith(data, 0xFF, 0xD8, 0xFF),
        ".webp" => data.Length > 12 && StartsWith(data, 0x52, 0x49, 0x46, 0x46) && data[8] == 0x57 && data[9] == 0x45 && data[10] == 0x42 && data[11] == 0x50,
        ".docx" or ".xlsx" or ".zip" => StartsWith(data, 0x50, 0x4B, 0x03, 0x04),
        ".doc" or ".xls" => StartsWith(data, 0xD0, 0xCF, 0x11, 0xE0),
        // Metin dosyası: ilk 4 KB içinde ikili (NUL) bayt olmamalı.
        ".txt" or ".csv" => !data.AsSpan(0, Math.Min(4096, data.Length)).Contains((byte)0),
        _ => false,
    };
}
