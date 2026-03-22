using PersonalBudget.Application.DTOs.Household;

namespace PersonalBudget.Application.Interfaces;

public interface IHouseholdService
{
    Task<IReadOnlyList<HouseholdListItemDto>> ListForUserAsync(Guid userId);
    Task<IReadOnlyList<HouseholdMemberProfileResponseDto>> GetProfilesAsync(Guid userId, Guid householdId);
    Task<HouseholdMemberProfileResponseDto> CreateJointProfileAsync(Guid userId, Guid householdId, string displayName);
    Task<string> CreateInviteAsync(Guid inviterUserId, Guid householdId, string inviteeEmail);
    Task AcceptInviteAsync(Guid userId, string token);
    Task<IReadOnlyList<HouseholdProfileSummaryRow>> GetSummaryByProfileAsync(
        Guid userId, Guid householdId, int month, int year);
}
