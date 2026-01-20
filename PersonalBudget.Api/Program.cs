

using PersonalBudget.Application.Services;
using PersonalBudget.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddScoped<AccountService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";

builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var app = builder.Build();

app.UseCors("AllowAll");

app.MapOpenApi();

app.MapControllers();
app.MapAccountEndpoints();
app.MapGet("/health", () => "OK");

app.Run();