public interface ITransactionService
{
    Task<Guid> CreateAsync(CreateTransactionCommand command);
    Task CompleteAsync(CompleteTransactionCommand command);
    Task CancelAsync(CancelTransactionCommand command);
    Task<IEnumerable<Transaction>> GetByAccountAsync(GetTransactionsByAccountQuery query);
    Task<IEnumerable<GetAllTransactionByUserResponse>> GetByUserAsync(GetAllTransactionByUserQuery query);
    Task<IEnumerable<GetAllTransactionByUserResponse>> GetByUserAndMonthAsync(GetAllTransactionByUserAndMonthQuery query);
}
