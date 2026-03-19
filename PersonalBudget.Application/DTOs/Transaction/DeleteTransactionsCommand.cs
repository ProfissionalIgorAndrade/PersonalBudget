public record DeleteTransactionsCommand(Guid UserId, IReadOnlyList<Guid> TransactionIds);
