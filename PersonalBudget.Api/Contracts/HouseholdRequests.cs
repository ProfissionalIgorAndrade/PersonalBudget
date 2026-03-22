namespace PersonalBudget.Api.Contracts;

public record CreateHouseholdInviteRequest(Guid HouseholdId, string InviteeEmail);

public record AcceptInviteRequest(string Token);

/// <summary>Perfil de correspondente compartilhado (sem usuário), para atribuição de lançamentos.</summary>
public record CreateHouseholdMemberProfileRequest(string DisplayName);
