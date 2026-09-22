using System.Net;
using System.Text.Json;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Localization;
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

        string language = LanguageResolver.Resolve(context.Request.Headers.AcceptLanguage.ToString());

        object payload;

        switch (exception)
        {
            case ValidationException validationException:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                Dictionary<string, string[]> translatedErrors = validationException.Errors.ToDictionary(
                    e => e.Key,
                    e => e.Value.Select(message => MessageTranslator.Translate(message, language)).ToArray());
                payload = new { title = MessageTranslator.Translate("Doğrulama hatası.", language), errors = translatedErrors };
                break;
            case NotFoundException notFoundException:
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                payload = new { title = MessageTranslator.Translate(notFoundException.Message, language) };
                break;
            case ForbiddenAccessException forbiddenException:
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                payload = new { title = MessageTranslator.Translate(forbiddenException.Message, language) };
                break;
            case ConflictException conflictException:
                context.Response.StatusCode = (int)HttpStatusCode.Conflict;
                payload = new { title = MessageTranslator.Translate(conflictException.Message, language) };
                break;
            default:
                _logger.LogError(exception, "İşlenmeyen bir hata oluştu.");
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                payload = new { title = MessageTranslator.Translate("Beklenmeyen bir sunucu hatası oluştu.", language) };
                break;
        }

        await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}
