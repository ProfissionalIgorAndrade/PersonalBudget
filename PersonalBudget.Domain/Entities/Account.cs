public class Account
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }

    public decimal Balance { get; private set; }

    public Account(string name, decimal initialBalance)
    {
        Id = Guid.NewGuid();
        Name = name;
        Balance = initialBalance;
    }

    public void Credit(decimal amount)
    {
        Balance += amount;
    }

    public void Debit(decimal amount)
    {
        if(Balance < 0)
            throw new InvalidOperationException("Insufficient funds."); 
        Balance -= amount;
    }
}