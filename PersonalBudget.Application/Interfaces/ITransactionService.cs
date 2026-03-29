public interface ITransactionService
{
    Task<Guid> CreateAsync(CreateTransactionCommand command);
    Task<Transaction> GetByIdAsync(Guid transactionId, Guid householdId);
    Task<IEnumerable<GetAllTransactionByUserResponse>> GetByHouseholdAsync(GetAllTransactionByHouseholdQuery query);
    Task<IEnumerable<GetAllTransactionByUserResponse>> GetByHouseholdAndMonthAsync(GetAllTransactionByHouseholdAndMonthQuery query);
    Task<PaginatedTransactionsResult> GetByHouseholdAndMonthPagedAsync(GetAllTransactionByHouseholdAndMonthQuery query);
    Task<IEnumerable<GetAllTransactionByUserResponse>> GetByAccountAndMonthAsync(GetTransactionsByAccountAndMonthYearQuery query);
    Task<PaginatedTransactionsResult> GetByAccountAndMonthPagedAsync(GetTransactionsByAccountAndMonthYearQuery query, int page);
    Task<IEnumerable<GetAllTransactionByUserResponse>> GetTransactionByCreditCardStatementAndMonthQuery(GetAllTransactionByCreditCardStatementAndMonthYearQuery query);
    Task UpdateStatusAsync(UpdateTransactionStatusCommand command);
    Task UpdateAsync(UpdateTransactionCommand command);
    Task<DeleteTransactionsResult> DeleteAsync(DeleteTransactionCommand command);
    Task<DeleteTransactionsResult> DeleteManyAsync(DeleteTransactionsCommand command);
}
