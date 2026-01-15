using PersonalBudget.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddSingleton<AccountService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    app.UseHttpsRedirection();
}

// Endpoints
app.MapHealthEndpoints();
app.MapAccountEndpoints();

app.Run();