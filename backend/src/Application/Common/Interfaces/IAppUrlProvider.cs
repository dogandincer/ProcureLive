namespace ProcureLive.Application.Common.Interfaces;

/// <summary>E-postalardaki bağlantılar için ön yüz adresi (yapılandırmadan; istek başlığından alınmaz).</summary>
public interface IAppUrlProvider
{
    string FrontendBaseUrl { get; }
}
