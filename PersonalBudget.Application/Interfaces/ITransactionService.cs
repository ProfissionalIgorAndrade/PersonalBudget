public interface ITransactionService
{
    Task<Guid> CreateAsync(CreateTransactionCommand command);
    Task UpdateStatusAsync(UpdateTransactionStatusCommand command);
    Task<DeleteTransactionsResult> DeleteManyAsync(DeleteTransactionsCommand command);
    Task<IEnumerable<GetAllTransactionByUserResponse>> GetByUserAsync(GetAllTransactionByUserQuery query);
    Task<Transaction> GetByIdAsync(Guid transactionId);
    Task<IEnumerable<GetAllTransactionByUserResponse>> GetByUserAndMonthAsync(GetAllTransactionByUserAndMonthQuery query);
    Task<PaginatedTransactionsResult> GetByUserAndMonthPagedAsync(GetAllTransactionByUserAndMonthQuery query);
    Task<IEnumerable<GetAllTransactionByUserResponse>> GetByAccountAndMonthAsync(GetTransactionsByAccountAndMonthYearQuery query);
    Task<PaginatedTransactionsResult> GetByAccountAndMonthPagedAsync(GetTransactionsByAccountAndMonthYearQuery query, int page);
    Task<IEnumerable<GetAllTransactionByUserResponse>> GetTransactionByCreditCardStatementAndMonthQuery(GetAllTransactionByCreditCardStatementAndMonthYearQuery query);
}
