using System.Net;
using System.Text.Json;
using PersonalBudget.Api.Contracts;

namespace PersonalBudget.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var (statusCode, message) = ex switch
        {
            DomainException domainEx => (HttpStatusCode.BadRequest, domainEx.Message),
            ArgumentException argEx => (HttpStatusCode.BadRequest, argEx.Message),
            _ => (HttpStatusCode.InternalServerError, _env.IsDevelopment() ? ex.Message : "Ocorreu um erro interno.")
        };

        if (statusCode == HttpStatusCode.InternalServerError)
            _logger.LogError(ex, "Unhandled exception");

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var response = ApiResponse<object?>.Fail(message);
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }
}
