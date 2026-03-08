public interface ITransactionService
{
    Task<Guid> CreateAsync(CreateTransactionCommand command);
    Task UpdateStatusAsync(UpdateTransactionStatusCommand command);
    Task<IEnumerable<GetAllTransactionByUserResponse>> GetByUserAsync(GetAllTransactionByUserQuery query);
    Task<IEnumerable<GetAllTransactionByUserResponse>> GetByUserAndMonthAsync(GetAllTransactionByUserAndMonthQuery query);
}
