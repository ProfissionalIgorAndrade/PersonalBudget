namespace PersonalBudget.Application.Interfaces;

public interface IHouseholdProvisioningService
{
    /// <summary>Cria lar padrão, membership como Owner e perfis (usuário + Família).</summary>
    Task ProvisionNewUserAsync(Guid userId, string displayName);
}
