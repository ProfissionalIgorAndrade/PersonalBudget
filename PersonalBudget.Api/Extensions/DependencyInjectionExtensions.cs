namespace PersonalBudget.Api.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddApplicationDependencies(
        this IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();

        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<ICategoryService, CategoryService>();
        
        services.AddScoped<IPasswordHasher, PasswordHasher>();

        return services;
    }
}

