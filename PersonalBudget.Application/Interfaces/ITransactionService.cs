using PersonalBudget.Application.DTOs.Transaction;

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
    Task<TransactionsCategoryGroupResponse> GetGroupedByCategoryForMonthAsync(Guid householdId, int month, int year);
    Task<IReadOnlyList<ActiveInstallmentGroupDto>> GetActiveInstallmentsAsync(Guid householdId, DateTime upTo);
    Task UpdateStatusAsync(UpdateTransactionStatusCommand command);
    Task UpdateAsync(UpdateTransactionCommand command);
    Task UpdateRecurringAsync(UpdateRecurringTransactionCommand command);
    Task<DeleteTransactionsResult> DeleteAsync(DeleteTransactionCommand command);
    Task<DeleteTransactionsResult> DeleteManyAsync(DeleteTransactionsCommand command);
}
