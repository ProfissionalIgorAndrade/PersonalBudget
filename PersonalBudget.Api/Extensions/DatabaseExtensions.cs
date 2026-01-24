using Microsoft.EntityFrameworkCore;

namespace PersonalBudget.Api.Extensions;

public static class DatabaseExtensions
{
    public static IServiceCollection AddDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseInMemoryDatabase("PersonalBudgetDb");
        });

        return services;
    }
}
