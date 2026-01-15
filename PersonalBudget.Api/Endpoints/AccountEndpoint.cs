public static class AccountEndpoints
{
    public static void MapAccountEndpoints(this WebApplication app)
    {
        app.MapPost("/api/accounts", (AccountService accountService, CreateAccountRequest request) =>
        {
            var result = accountService.CreateAccount(request.Name, request.InitialBalance);
            return Results.Ok(result);
        });

        app.MapGet("/api/accounts", (AccountService service) =>
        {
            return Results.Ok(service.GetAllAccounts());
        });
    }
}