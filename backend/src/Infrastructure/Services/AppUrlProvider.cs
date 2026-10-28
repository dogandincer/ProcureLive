using Microsoft.Extensions.Configuration;
using ProcureLive.Application.Common.Interfaces;

namespace ProcureLive.Infrastructure.Services;

/// <summary>App:FrontendBaseUrl, yoksa ilk izinli CORS kaynağı, yoksa yerel geliştirme adresi.</summary>
public sealed class AppUrlProvider : IAppUrlProvider
{
    public AppUrlProvider(IConfiguration configuration)
    {
        FrontendBaseUrl = (configuration["App:FrontendBaseUrl"] ?? configuration["Cors:AllowedOrigins:0"] ?? "http://localhost:3000").TrimEnd('/');
    }

    public string FrontendBaseUrl { get; }
}
