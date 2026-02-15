public interface ITransactionQueryRepository
{
    Task<IReadOnlyList<GetAllTransactionByUserResponse>> GetByUserAsync(Guid userId);
    Task<IReadOnlyList<GetAllTransactionByUserResponse>> GetByUserAndMonthAsync(Guid userId, int month, int year);
}
