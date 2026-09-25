namespace PersonalBudget.Application.DTOs.Account;

public record AccountResponse(
    Guid Id,
    string Bank,
    string Agency,
    string AccountNumber,
    decimal Balance,
    Guid? MemberProfileId,
    string? MemberName,
    string DisplayName,
    bool IsActive,
    DateTime CreatedAt,
    /// <summary>"Checking" ou "Savings".</summary>
    string Kind = "Checking",
    /// <summary>Conta corrente da caixinha. Null para conta corrente.</summary>
    Guid? ParentAccountId = null,
    /// <summary>Nome da caixinha. Null para conta corrente.</summary>
    string? Name = null
);
