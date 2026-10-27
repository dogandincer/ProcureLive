using Microsoft.Extensions.Hosting;
using ProcureLive.Application.Common.Interfaces;

namespace ProcureLive.Infrastructure.Services;

/// <summary>Dosyaları {ContentRoot}/wwwroot/uploads altına yazar; WebApi UseStaticFiles ile sunar.</summary>
public sealed class LocalFileStorage : IFileStorage
{
    private const string UploadRoot = "uploads";
    private readonly string _webRoot;

    public LocalFileStorage(IHostEnvironment environment)
    {
        _webRoot = Path.Combine(environment.ContentRootPath, "wwwroot");
    }

    public async Task<string> SaveAsync(string folder, string fileName, Stream content, CancellationToken cancellationToken)
    {
        // Dışarıdan gelen ad hiçbir zaman yol olarak kullanılmaz: klasör sabit, ad sadece güvenli karakterler.
        string safeFolder = Sanitize(folder);
        string safeName = Sanitize(fileName);
        string directory = Path.Combine(_webRoot, UploadRoot, safeFolder);
        Directory.CreateDirectory(directory);

        string fullPath = Path.Combine(directory, safeName);
        await using (FileStream file = File.Create(fullPath))
        {
            await content.CopyToAsync(file, cancellationToken);
        }

        return $"/{UploadRoot}/{safeFolder}/{safeName}";
    }

    public Task DeleteAsync(string relativeUrl, CancellationToken cancellationToken)
    {
        if (!relativeUrl.StartsWith($"/{UploadRoot}/", StringComparison.Ordinal))
        {
            return Task.CompletedTask; // harici adres (eski URL tabanlı logolar) — dokunulmaz
        }

        string fullPath = Path.GetFullPath(Path.Combine(_webRoot, relativeUrl.TrimStart('/')));
        string root = Path.GetFullPath(Path.Combine(_webRoot, UploadRoot));
        if (fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase) && File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    private static string Sanitize(string value) =>
        new(value.Where(ch => char.IsAsciiLetterOrDigit(ch) || ch is '-' or '_' or '.').ToArray());
}
