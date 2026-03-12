public interface ITransactionService
{
    Task<Guid> CreateAsync(CreateTransactionCommand command);
    Task UpdateStatusAsync(UpdateTransactionStatusCommand command);
    Task UpdateStatusToCreditCardStatementAsync(Guid userId, UpdateTransactionStatusToCreditCardStatementCommand command);
    Task<IEnumerable<GetAllTransactionByUserResponse>> GetByUserAsync(GetAllTransactionByUserQuery query);
    Task<Transaction> GetByIdAsync(Guid transactionId);
    Task<IEnumerable<GetAllTransactionByUserResponse>> GetByUserAndMonthAsync(GetAllTransactionByUserAndMonthQuery query);
    Task<IEnumerable<GetAllTransactionByUserResponse>> GetTransactionByCreditCardStatementAndMonthQuery(GetAllTransactionByCreditCardStatementAndMonthYearQuery query);
}
