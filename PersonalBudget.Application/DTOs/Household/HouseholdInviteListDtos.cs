namespace PersonalBudget.Application.DTOs.Household;

/// <summary>Convite pendente recebido pelo e-mail do usuário logado (aceitar via token).</summary>
public record ReceivedPendingInviteDto(
    Guid InviteId,
    Guid HouseholdId,
    string HouseholdName,
    string Token,
    DateTime ExpiresAtUtc,
    DateTime CreatedAtUtc,
    bool IsExpired,
    Guid InviterUserId,
    string? InviterName);

/// <summary>Convite pendente enviado por este lar (visão do anfitrião / membros).</summary>
public record SentPendingInviteDto(
    Guid InviteId,
    string InviteeEmail,
    DateTime ExpiresAtUtc,
    DateTime CreatedAtUtc,
    bool IsExpired,
    Guid InviterUserId,
    string? InviterName);
