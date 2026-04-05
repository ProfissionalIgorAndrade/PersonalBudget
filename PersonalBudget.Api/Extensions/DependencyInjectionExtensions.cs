using PersonalBudget.Application.Interfaces;
using PersonalBudget.Application.Services.TransactionCreation;
using PersonalBudget.Infrastructure.Repositories;

namespace PersonalBudget.Api.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddApplicationDependencies(
        this IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IHouseholdRepository, HouseholdRepository>();
        services.AddScoped<IHouseholdMembershipRepository, HouseholdMembershipRepository>();
        services.AddScoped<IHouseholdMemberProfileRepository, HouseholdMemberProfileRepository>();
        services.AddScoped<IHouseholdInviteRepository, HouseholdInviteRepository>();
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ICreditCardRepository, CreditCardRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<ITransactionQueryRepository, TransactionQueryRepository>();
        services.AddScoped<ICreditCardStatementRepository, CreditCardStatementRepository>();

        services.AddScoped<IActiveHouseholdResolver, ActiveHouseholdResolver>();
        services.AddScoped<IHouseholdProvisioningService, HouseholdProvisioningService>();
        services.AddScoped<IHouseholdService, HouseholdService>();

        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<ICreditCardService, CreditCardService>();
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<ICreditCardStatementService, CreditCardStatementService>();
        services.AddScoped<ICreditCardImportService, CreditCardImportService>();

        // Transaction creation strategies
        services.AddScoped<ITransactionCreationStrategy, AccountTransactionCreationStrategy>();
        services.AddScoped<ITransactionCreationStrategy, CreditCardTransactionCreationStrategy>();
        services.AddScoped<ITransactionCreationStrategy, TransferTransactionCreationStrategy>();

        services.AddScoped<IPasswordHasher, PasswordHasher>();

        return services;
    }
}

