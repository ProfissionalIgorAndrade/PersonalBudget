public sealed class BankAccountNumber
{
    public string Value { get; }

    public BankAccountNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Número da conta é obrigatório.");

        Value = value.Trim();
    }

    private BankAccountNumber() { } // EF
}
