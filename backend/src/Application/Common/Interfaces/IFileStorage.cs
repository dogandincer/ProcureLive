namespace ProcureLive.Application.Common.Interfaces;

/// <summary>
/// Yüklenen dosyaların saklanması. Şimdilik yerel disk (wwwroot/uploads) — bulut depolamaya geçişte yalnızca
/// Infrastructure değişir. Dönen değer istemcinin kullanacağı göreli adrestir ("/uploads/logos/...").
/// </summary>
public interface IFileStorage
{
    Task<string> SaveAsync(string folder, string fileName, Stream content, CancellationToken cancellationToken);

    Task DeleteAsync(string relativeUrl, CancellationToken cancellationToken);
}
