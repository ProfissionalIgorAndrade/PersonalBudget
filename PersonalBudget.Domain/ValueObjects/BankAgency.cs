public sealed class BankAgency
{
    public string Value { get; }

    public BankAgency(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Número da agência é obrigatório.");

        Value = value.Trim();
    }

    private BankAgency() { } // EF
}
