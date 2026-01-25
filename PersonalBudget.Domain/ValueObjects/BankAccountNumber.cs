public sealed class BankAccountNumber
{
    public string Value { get; }

    public BankAccountNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Account number is required.");

        Value = value.Trim();
    }

    private BankAccountNumber() { } // EF
}
