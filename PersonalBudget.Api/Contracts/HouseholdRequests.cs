namespace PersonalBudget.Api.Contracts;

public record CreateHouseholdInviteRequest(Guid HouseholdId, string InviteeEmail);

public record AcceptInviteRequest(string Token);

/// <summary>Perfil de correspondente compartilhado (sem usuário), para atribuição de lançamentos.</summary>
public record CreateHouseholdMemberProfileRequest(string DisplayName);

/// <summary>Remove um correspondente após migrar lançamentos (e contas/cartões quando for fusão de dois perfis vinculados).</summary>
public record DeleteHouseholdMemberProfileRequest(Guid RemoveProfileId, Guid MergeIntoProfileId);
