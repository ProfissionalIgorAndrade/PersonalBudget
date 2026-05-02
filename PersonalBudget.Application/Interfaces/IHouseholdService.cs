using PersonalBudget.Application.DTOs.Household;

namespace PersonalBudget.Application.Interfaces;

public interface IHouseholdService
{
    Task<IReadOnlyList<HouseholdListItemDto>> ListForUserAsync(Guid userId);
    Task<IReadOnlyList<HouseholdMemberProfileResponseDto>> GetProfilesAsync(Guid userId, Guid householdId);
    Task<HouseholdMemberProfileResponseDto> CreateJointProfileAsync(Guid userId, Guid householdId, string displayName);
    /// <summary>Migra lançamentos (e contas/cartões quando aplicável) para outro perfil e remove o perfil antigo.</summary>
    Task DeleteProfileAndMergeAsync(Guid userId, Guid householdId, Guid removeProfileId, Guid mergeIntoProfileId);
    Task<string> CreateInviteAsync(Guid inviterUserId, Guid householdId, string inviteeEmail);
    Task<IReadOnlyList<ReceivedPendingInviteDto>> ListMyPendingInvitesAsync(Guid userId);
    Task<IReadOnlyList<SentPendingInviteDto>> ListPendingInvitesForHouseholdAsync(Guid userId, Guid householdId);
    Task<IReadOnlyList<HouseholdMemberDto>> ListInvitedMembersAsync(Guid userId, Guid householdId);
    Task AcceptInviteAsync(Guid userId, string token);
    Task<IReadOnlyList<HouseholdProfileSummaryRow>> GetSummaryByProfileAsync(
        Guid userId, Guid householdId, int month, int year);
}
