namespace PersonalBudget.Application.DTOs.Account;

public record AccountResponse(
    Guid Id,
    string Bank,
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
    /// <summary>Nome da caixinha ou apelido da conta corrente. Null quando ausente.</summary>
    string? Name = null,
    /// <summary>Meta da caixinha. Null quando não há meta.</summary>
    decimal? SavingsGoal = null
);
