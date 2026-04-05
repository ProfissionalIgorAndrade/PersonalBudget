public record ImportCreditCardCsvResult(
    int Imported,
    int Skipped,
    List<string> Errors
);
