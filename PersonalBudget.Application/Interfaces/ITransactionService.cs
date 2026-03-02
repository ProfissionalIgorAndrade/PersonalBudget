public interface ITransactionService
{
    Task<Guid> CreateAsync(CreateTransactionCommand command);
    Task<Guid> CreateTransferAsync(CreateTransactionCommand command);
    Task UpdateStatusAsync(UpdateTransactionStatusCommand command);
    Task<IEnumerable<Transaction>> GetByAccountAsync(GetTransactionsByAccountQuery query);
    Task<IEnumerable<GetAllTransactionByUserResponse>> GetByUserAsync(GetAllTransactionByUserQuery query);
    Task<IEnumerable<GetAllTransactionByUserResponse>> GetByUserAndMonthAsync(GetAllTransactionByUserAndMonthQuery query);
}
