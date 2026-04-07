namespace PersonalBudget.Application.DTOs.Transaction;

public record CategoryTotalDto(
    Guid? CategoryId,
    string? CategoryName,
    decimal Total,
    int TransactionCount
);

public record TransactionsCategoryGroupResponse(
    IReadOnlyList<CategoryTotalDto> Expenses,
    IReadOnlyList<CategoryTotalDto> Incomes
);
