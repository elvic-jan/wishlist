using System.Text.Json;
using Wishlist.Application.Localization;
using Wishlist.Domain.Exceptions;

namespace Wishlist.API.Middleware;

public class ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");
            await WriteErrorAsync(context, ex);
        }
    }

    private static Task WriteErrorAsync(HttpContext context, Exception ex)
    {
        var status = ex switch
        {
            NotFoundException => StatusCodes.Status404NotFound,
            ForbiddenException => StatusCodes.Status403Forbidden,
            ConflictException => StatusCodes.Status409Conflict,
            GoneException => StatusCodes.Status410Gone,
            TooManyRequestsException => StatusCodes.Status429TooManyRequests,
            DomainException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };
        // In the language the app is showing (its Accept-Language).
        var language = RequestLanguage.Of(context);
        var message = ex is DomainException error
            ? Texts.T(language, error)
            : Texts.T(language, "An unexpected error occurred.");

        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsync(JsonSerializer.Serialize(new { error = message }));
    }
}
