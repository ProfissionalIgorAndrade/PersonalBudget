namespace PersonalBudget.Api.Contracts;

/// <summary>
/// Envelope padrão de resposta da API: status, mensagem e dados (quando houver).
/// </summary>
public record ApiResponse<T>(
    bool Success,
    string Message,
    T? Data = default
)
{
    public static ApiResponse<T> Ok(T? data, string message = "Sucesso") =>
        new(true, message, data);

    public static ApiResponse<T> Fail(string message) =>
        new(false, message, default);
}
