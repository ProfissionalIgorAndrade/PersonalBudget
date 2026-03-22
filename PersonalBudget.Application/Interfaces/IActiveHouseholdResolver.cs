namespace PersonalBudget.Application.Interfaces;

/// <summary>
/// Resolve o lar ativo: header X-Household-Id (se válido e o usuário for membro) ou o primeiro lar do usuário.
/// </summary>
public interface IActiveHouseholdResolver
{
    Task<Guid> ResolveAsync(Guid userId, Guid? householdIdFromHeader);
}
