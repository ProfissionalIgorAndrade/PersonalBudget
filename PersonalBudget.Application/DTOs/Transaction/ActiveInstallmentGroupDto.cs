namespace PersonalBudget.Application.DTOs.Transaction;

public record ActiveInstallmentGroupDto(
    string GroupKey,
    string Description,
    Guid? CreditCardId,
    string? CreditCardName,
    Guid? CategoryId,
    string? CategoryName,
    decimal InstallmentAmount,
    decimal TotalAmount,
    int InstallmentsPaid,
    int InstallmentsTotal,
    decimal RemainingAmount,
    string? NextDueDate,
    IReadOnlyList<GetAllTransactionByUserResponse> Items
);
