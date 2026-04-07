namespace PersonalBudget.Application.DTOs.Account;

public record AccountSummaryItem(Guid Id, string Name, string Bank, decimal Balance);

public record AccountsSummaryResponse(
    decimal TotalBalance,
    IReadOnlyList<AccountSummaryItem> Accounts
);
