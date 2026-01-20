using PersonalBudget.Application.Services;

namespace PersonalBudget.Api.Endpoints;

/// <summary>
/// Account management endpoints for creating and retrieving accounts
/// </summary>
public static class AccountEndpoints
{
    /// <summary>
    /// Maps all account-related endpoints
    /// </summary>
    public static void MapAccountEndpoints(this WebApplication app)
    {
        /// <summary>
        /// Creates a new account
        /// </summary>
        /// <param name="accountService">Service to handle account creation</param>
        /// <param name="request">Request containing account name and initial balance</param>
        /// <returns>Created account details</returns>
        app.MapPost("/api/accounts", (AccountService accountService, CreateAccountRequest request) =>
        {
            var result = accountService.CreateAccount(request.Name, request.InitialBalance);
            return Results.Ok(result);
        })
        .WithName("CreateAccount")
        .Produces(200);

        /// <summary>
        /// Retrieves all accounts
        /// </summary>
        /// <param name="service">Service to retrieve accounts</param>
        /// <returns>List of all accounts</returns>
        app.MapGet("/api/accounts", (AccountService service) =>
        {
            return Results.Ok(service.GetAllAccounts());
        })
        .WithName("GetAllAccounts")
        .Produces(200);
    }
}