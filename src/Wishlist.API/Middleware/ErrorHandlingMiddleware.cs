using System.Text.Json;
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
        var (status, message) = ex switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, ex.Message),
            ForbiddenException => (StatusCodes.Status403Forbidden, ex.Message),
            ConflictException => (StatusCodes.Status409Conflict, ex.Message),
            GoneException => (StatusCodes.Status410Gone, ex.Message),
            DomainException => (StatusCodes.Status400BadRequest, ex.Message),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
        };

        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsync(JsonSerializer.Serialize(new { error = message }));
    }
}
