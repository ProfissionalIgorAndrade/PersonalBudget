using DotNet.Testcontainers.Builders;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace PersonalBudget.Integration.Tests;

public class IntegrationTestFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder()
        .WithDatabase("personal_budget_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
        .Build();

    public async Task InitializeAsync() => await _db.StartAsync();

    public new async Task DisposeAsync() => await _db.DisposeAsync();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _db.GetConnectionString(),
                ["Jwt:Key"]              = "THIS_IS_A_SUPER_SECRET_KEY_WITH_AT_LEAST_32_CHARS",
                ["Jwt:Issuer"]           = "PersonalBudget",
                ["Jwt:Audience"]         = "PersonalBudget.Web",
                ["Jwt:ExpiresInMinutes"] = "60"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Replace the DbContext registered by DatabaseExtensions with one
            // pointing to the Testcontainers PostgreSQL instance.
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor is not null)
                services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(_db.GetConnectionString()));
        });
    }
}
