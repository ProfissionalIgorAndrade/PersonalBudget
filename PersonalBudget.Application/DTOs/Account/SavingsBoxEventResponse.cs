namespace PersonalBudget.Application.DTOs.Account;

public record SavingsBoxEventResponse(
    Guid Id,
    /// <summary>"Created" ou "Deleted".</summary>
    string Kind,
    Guid AccountId,
    /// <summary>Nome da caixinha na hora do evento.</summary>
    string BoxName,
    string? Reason,
    /// <summary>Saldo movido na exclusão; 0 na criação.</summary>
    decimal Amount,
    Guid? DestinationAccountId,
    string? DestinationName,
    DateTime OccurredAt);
