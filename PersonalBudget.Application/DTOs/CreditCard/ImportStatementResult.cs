public enum ImportStatementRowStatus
{
    /// <summary>Não existe na fatura: criada (ou seria criada, na prévia).</summary>
    New = 1,
    /// <summary>Já existe no lar (por id) ou na fatura (por conteúdo): ignorada.</summary>
    Existing = 2,
    /// <summary>Falhou ao gravar; a mensagem diz o motivo.</summary>
    Error = 3
}

public record ImportStatementRowResult(int Index, ImportStatementRowStatus Status, string? Message);

public record ImportStatementResult(
    int Created,
    int Skipped,
    List<string> Errors,
    List<ImportStatementRowResult> Rows
);
