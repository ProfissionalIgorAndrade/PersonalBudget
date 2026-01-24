using PersonalBudget.Application.Services;

namespace PersonalBudget.Api.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddApplicationDependencies(
        this IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IAccountUserRepository, AccountUserRepository>();

        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IAccountService, AccountService>();

        services.AddScoped<IPasswordHasher, PasswordHasher>();

        return services;
    }
}

