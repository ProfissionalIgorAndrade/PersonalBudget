public record ImportCreditCardCsvCommand(
    Guid UserId,
    Guid HouseholdId,
    Guid CreditCardId,
    string CsvContent
);
