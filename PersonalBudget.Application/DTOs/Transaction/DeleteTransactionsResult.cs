public record DeleteTransactionsResult(
    int DeletedCount,
    int SkippedCount,
    IReadOnlyList<Guid> SkippedIds
);
