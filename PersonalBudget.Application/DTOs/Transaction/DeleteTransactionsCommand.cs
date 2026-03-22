public record DeleteTransactionsCommand(Guid HouseholdId, IReadOnlyList<Guid> TransactionIds);
