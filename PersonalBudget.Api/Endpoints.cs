namespace PersonalBudget.Api;

public static class Endpoints
{
    public static void MapHealthEndpoints(this WebApplication app)
    {
        app.MapGet("/api/health", () => "OK v1")
            .WithName("Health Check")
            .WithOpenApi();
    }
}
