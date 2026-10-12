public record ImportStatementRowRequest(
    /// <summary>Coluna id_sistema do CSV. Texto livre: só conta se for o Id de um lançamento do lar.</summary>
    string? ExternalId,
    string  Description,
    decimal Amount,
    string  Date,
    TransactionType Type,
    Guid?   CategoryId,
    Guid?   AttributionProfileId,
    string? Observations
);

public record ImportStatementRequest(
    int StatementMonth,
    int StatementYear,
    /// <summary>Quando true, apenas classifica as linhas (nova/existente) sem gravar nada.</summary>
    bool DryRun,
    List<ImportStatementRowRequest> Rows
);
