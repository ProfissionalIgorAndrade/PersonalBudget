namespace PersonalBudget.Application.DTOs.Account;

/// <summary>Cria uma caixinha vinculada a uma conta corrente.</summary>
public record CreateSavingsBoxRequest(Guid ParentAccountId, string Name);
