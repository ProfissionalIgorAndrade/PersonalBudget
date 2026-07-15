public record UpdateCreditCardRequest(
    string Name,
    decimal Limit,
    int ClosingDay,
    int DueDay,
    string? Color = null
);
