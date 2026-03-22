namespace PersonalBudget.Application.DTOs.Household;

public record HouseholdMemberProfileResponseDto(
    Guid Id,
    string DisplayName,
    string Kind,
    Guid? UserId);
