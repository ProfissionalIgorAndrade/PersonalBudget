public record BulkImportRowRequest(
    Guid?   ExternalId,
    string  Description,
    decimal Amount,
    string  Date,
    TransactionType Type,
    Guid?   CategoryId,
    Guid?   AttributionProfileId,
    string? Observations
);

public record BulkImportTransactionRequest(
    List<BulkImportRowRequest> Rows,
    Guid DefaultAccountId
);

public record BulkImportTransactionResult(int Created, int Skipped, List<string> Errors);
