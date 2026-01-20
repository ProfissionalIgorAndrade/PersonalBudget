

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

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    app.MapOpenApi();
    app.UseHttpsRedirection();
}

app.MapControllers();

// Endpoints
app.MapAccountEndpoints();
app.MapGet("/health", () => "OK");

app.UseCors("AllowAll");

app.Run();