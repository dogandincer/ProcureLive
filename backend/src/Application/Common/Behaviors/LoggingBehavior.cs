using MediatR;
using Microsoft.Extensions.Logging;
using ProcureLive.Application.Common.Interfaces;

namespace ProcureLive.Application.Common.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
    private readonly ICurrentUserService _currentUserService;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger, ICurrentUserService currentUserService)
    {
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        string requestName = typeof(TRequest).Name;

        _logger.LogInformation(
            "ProcureLive Request: {RequestName} kullanıcı {UserId} ({Email}) tarafından çalıştırılıyor",
            requestName,
            _currentUserService.UserId,
            _currentUserService.Email);

        return await next();
    }
}
