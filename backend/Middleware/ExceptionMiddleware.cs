using System.Net;
using System.Text.Json;
using LearnPath.API.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace LearnPath.API.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception on {Method} {Path}",
                context.Request.Method, context.Request.Path);

            if (context.Response.HasStarted)
            {
                _logger.LogWarning(
                    "Response already started; cannot write an error body for {Path}.",
                    context.Request.Path);
                throw;
            }

            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        var (statusCode, message) = ex switch
        {
            // Known, expected failures — surface the exact reason to the user.
            UnauthorizedAccessException => (HttpStatusCode.Forbidden,
                ex.Message),
            KeyNotFoundException => (HttpStatusCode.NotFound,
                ex.Message),
            ArgumentException => (HttpStatusCode.BadRequest,
                ex.Message),

            // Referential integrity violation — tell the user a dependency
            // prevents the operation, never leak the raw constraint text.
            DbUpdateException when IsForeignKeyViolation(ex) =>
                (HttpStatusCode.Conflict,
                 "The record could not be deleted because other data depends on it. Remove or reassign the dependent records first."),

            // Anything else — log the full exception above, show a safe message.
            _ => (HttpStatusCode.InternalServerError,
                 "An unexpected error occurred. Please try again."),
        };

        context.Response.StatusCode = (int)statusCode;
        var response = ApiResponse<object>.Fail(message);
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }

    private static bool IsForeignKeyViolation(Exception ex)
    {
        for (var current = ex; current is not null; current = current.InnerException)
        {
            if (current is SqlException { Number: 547 })   // FK constraint violation
                return true;
        }
        return false;
    }
}
