using System.Net;
using System.Text.Json;

namespace HrmsApi.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next   = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var (statusCode, title) = ex switch
        {
            ArgumentException or InvalidOperationException => (HttpStatusCode.BadRequest, "Bad Request"),
            UnauthorizedAccessException                    => (HttpStatusCode.Unauthorized, "Unauthorized"),
            KeyNotFoundException                            => (HttpStatusCode.NotFound, "Not Found"),
            _                                              => (HttpStatusCode.InternalServerError, "Internal Server Error")
        };

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode  = (int)statusCode;

        var problem = new
        {
            type     = $"https://httpstatuses.com/{(int)statusCode}",
            title,
            status   = (int)statusCode,
            detail   = ex.Message,
            instance = context.Request.Path.Value
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(problem));
    }
}

// Extension method for clean registration
public static class ExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
        => app.UseMiddleware<ExceptionMiddleware>();
}
