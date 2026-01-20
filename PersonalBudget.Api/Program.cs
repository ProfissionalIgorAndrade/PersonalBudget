

using PersonalBudget.Application.Services;
using PersonalBudget.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Personal Budget API", Version = "v1" });
});
builder.Services.AddControllers();
builder.Services.AddScoped<AccountService>();
builder.Services.AddSingleton<CategoryService>();


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
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Personal Budget API v1");
        c.RoutePrefix = "swagger";
    });
}
else
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Personal Budget API v1");
        c.RoutePrefix = "swagger";
    });
    app.UseHttpsRedirection();
}

app.MapControllers();

// Endpoints
app.MapAccountEndpoints();

app.UseCors("AllowAll");

app.Run();