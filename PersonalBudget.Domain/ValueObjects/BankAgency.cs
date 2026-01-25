public sealed class BankAgency
{
    public string Value { get; }

    public BankAgency(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Agency number is required.");

        Value = value.Trim();
    }

    private BankAgency() { } // EF
}
