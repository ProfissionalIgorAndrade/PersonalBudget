using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PersonalBudget.Api.Contracts;

namespace PersonalBudget.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
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
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var (statusCode, message) = ex switch
        {
            DomainException domainEx => (HttpStatusCode.BadRequest, domainEx.Message),
            ApplicationException appEx when appEx.Message.Contains("não pertence ao usuário") => (HttpStatusCode.Forbidden, appEx.Message),
            ApplicationException appEx => (HttpStatusCode.BadRequest, appEx.Message),
            ArgumentException argEx => (HttpStatusCode.BadRequest, argEx.Message),
            DbUpdateException dbEx => (HttpStatusCode.InternalServerError, $"Erro ao salvar no banco: {dbEx.InnerException?.Message ?? dbEx.Message}"),
            _ => (HttpStatusCode.InternalServerError, ex.Message)
        };

        if (statusCode == HttpStatusCode.InternalServerError)
            _logger.LogError(ex, "Unhandled exception");

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var response = ApiResponse<object?>.Fail(message);
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }
}
