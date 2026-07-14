namespace PersonalBudget.Application.DTOs.Account;

public record AccountResponse(
    Guid Id,
    string Bank,
    string Agency,
    string AccountNumber,
    decimal Balance,
    Guid? MemberProfileId,
    string? MemberName,
    string DisplayName,
    bool IsActive,
    DateTime CreatedAt
);
