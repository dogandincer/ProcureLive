using System.Net;
using System.Text.Json;
using ProcureLive.Application.Common.Exceptions;
using ValidationException = ProcureLive.Application.Common.Exceptions.ValidationException;

namespace ProcureLive.WebApi.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        object payload;

        switch (exception)
        {
            case ValidationException validationException:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                payload = new { title = "Doğrulama hatası.", errors = validationException.Errors };
                break;
            case NotFoundException notFoundException:
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                payload = new { title = notFoundException.Message };
                break;
            case ForbiddenAccessException forbiddenException:
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                payload = new { title = forbiddenException.Message };
                break;
            case ConflictException conflictException:
                context.Response.StatusCode = (int)HttpStatusCode.Conflict;
                payload = new { title = conflictException.Message };
                break;
            default:
                _logger.LogError(exception, "İşlenmeyen bir hata oluştu.");
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                payload = new { title = "Beklenmeyen bir sunucu hatası oluştu." };
                break;
        }

        await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}
