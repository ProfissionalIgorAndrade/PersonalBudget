using PersonalBudget.Application.Services;

namespace PersonalBudget.Api.Endpoints;
public static class TransactionEndpoint
{
    public static void MapTransactionEndpoints(this WebApplication app)
    {
        app.MapPost("/api/transactions", (TransactionService service, CreateTransactionRequest request
            ) =>
            {
                var transaction = service.Create(
                    request.AccountId,
                    request.Type,
                    request.Amount,
                    request.Status
                );

                return Results.Ok(transaction);
            });

        app.MapGet("/api/transactions", (TransactionService service) =>
        {
            return Results.Ok(service.GetAll());
        });

    }
}