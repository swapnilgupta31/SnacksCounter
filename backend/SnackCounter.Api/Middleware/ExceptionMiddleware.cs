using System.Text.Json;
using SnackCounter.Core.DTOs;
using SnackCounter.Core.Exceptions;

namespace SnackCounter.Api.Middleware;

public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, code) = ex switch
        {
            ConflictException => (409, "CONFLICT"),
            NotFoundException => (404, "NOT_FOUND"),
            BudgetExceededException => (422, "BUDGET_EXCEEDED"),
            InvalidStateTransitionException => (422, "INVALID_STATE"),
            ValidationException => (400, "VALIDATION_ERROR"),
            _ => (500, "INTERNAL_ERROR")
        };

        context.Response.StatusCode = statusCode;
        var error = new ApiError(code, ex.Message);
        await context.Response.WriteAsync(JsonSerializer.Serialize(error,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }
}
