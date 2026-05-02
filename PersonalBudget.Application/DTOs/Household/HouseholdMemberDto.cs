namespace PersonalBudget.Application.DTOs.Household;

public record HouseholdMemberDto(
    Guid UserId,
    string Name,
    string Email,
    string Role,
    DateTime JoinedAtUtc);
