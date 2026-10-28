namespace ProcureLive.Contracts.Files;

/// <summary>Yüklenen dosya: Url sunucunun /uploads altındaki adresi, FileName özgün ad (gösterim için).</summary>
public sealed record UploadedFileDto(string Url, string FileName, string ContentType, long SizeBytes);
