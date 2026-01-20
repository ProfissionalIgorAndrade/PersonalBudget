

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

app.UseCors("AllowAll");

app.Run();