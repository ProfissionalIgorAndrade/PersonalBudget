using Bogus;

namespace PersonalBudget.Infrastructure.Persistence.Seed;

public static class DatabaseSeeder
{
    /// <summary>
    /// Faker com locale pt_BR para nomes, datas e números realistas.
    /// Bogus: https://github.com/bchavez/Bogus
    /// </summary>
    private static readonly Faker Faker = new Faker("pt_BR");

    public static async Task<Guid> SeedAsync(AppDbContext context)
    {
        if (context.Users.Any())
            return Guid.Empty; // banco já populado

        // USER (nome e email aleatórios)
        var userName = Faker.Person.FullName;
        var userEmail = "email@email.com";
        var user = new User(
            name: userName,
            email: new Email(userEmail),
            passwordHash: new PasswordHasher().Hash("Email123@")
        );

        context.Users.Add(user);

        // ACCOUNT 1 (agência, número e saldo aleatórios)
        var account1 = Account.Create(
            userId: user.Id,
            bank: Bank.Nubank,
            agency: new BankAgency(Faker.Random.Int(1000, 9999).ToString()),
            number: new BankAccountNumber($"{Faker.Random.Int(100000, 999999)}-{Faker.Random.Int(1, 9)}"),
            initialBalance: new Money(100)
        );

        var account2 = Account.Create(
            userId: user.Id,
            bank: Bank.Itau,
            agency: new BankAgency(Faker.Random.Int(1000, 9999).ToString()),
            number: new BankAccountNumber($"{Faker.Random.Int(100000, 999999)}-{Faker.Random.Int(1, 9)}"),
            initialBalance: new Money(5000)
        );

        context.Accounts.Add(account1);
        context.Accounts.Add(account2);

        // CATEGORY
        var categoryExpenseMoradia = Category.Create(
            userId: user.Id,
            name: "Moradia",
            type: CategoryType.Expense
        );

        var categoryExpense2 = Category.Create(
            userId: user.Id,
            name: "Alimentação",
            type: CategoryType.Expense
        );

        var categoryExpense3 = Category.Create(
                    userId: user.Id,
                    name: "Diversão",
                    type: CategoryType.Expense
                );

        var categoryIncome1 = Category.Create(
                    userId: user.Id,
                    name: "Salário",
                    type: CategoryType.Income
                );

        var categoryIncome3 = Category.Create(
                    userId: user.Id,
                    name: "Investimentos",
                    type: CategoryType.Income
                );

        var categoryIncome4 = Category.Create(
                    userId: user.Id,
                    name: "Outros",
                    type: CategoryType.Income
                );

        context.Categories.Add(categoryExpense2);
        context.Categories.Add(categoryExpense3);
        context.Categories.Add(categoryExpenseMoradia);
        context.Categories.Add(categoryIncome1);
        context.Categories.Add(categoryIncome3);
        context.Categories.Add(categoryIncome4);

        // CREDIT CARD (nome e limite aleatórios, dias entre 1-28)
        var cardNames = new[] { "Visa Platinum", "Smiles Premium", "Ultravioleta Infinite", "Mastercard Gold", "Elo Nanquim" };
        var card1 = CreditCard.Create(
            userId: user.Id,
            accountId: account1.Id,
            name: Faker.PickRandom(cardNames),
            limit: 8000,
            closingDay: 30,
            dueDay: 10
        );

        var card2 = CreditCard.Create(
            userId: user.Id,
            accountId: account2.Id,
            name: Faker.PickRandom(cardNames),
            limit: 5000,
            closingDay: 28,
            dueDay: 06
        );

        context.CreditCards.Add(card1);
        context.CreditCards.Add(card2);

        // TRANSACTION
        var transactionExpense1 = Transaction.Create(
            userId: user.Id,
            accountId: account1.Id,
            amount: new Money(6800),
            type: TransactionType.Expense,
            paymentMethod: PaymentMethod.Account,
            description: "Aluguel",
            date: DateTime.UtcNow.AddDays(-1),
            categoryId: categoryExpenseMoradia.Id,
            creditCardId: null
        );

        var transactionExpense2 = Transaction.Create(
            userId: user.Id,
            accountId: account1.Id,
            amount: new Money(2500),
            type: TransactionType.Expense,
            paymentMethod: PaymentMethod.CreditCard,
            description: "Home teatcher LG - Amazon",
            date: DateTime.UtcNow.AddDays(-9),
            categoryId: categoryExpense2.Id,
            creditCardId: card1.Id
        );

        var transactionExpense3 = Transaction.Create(
            userId: user.Id,
            accountId: account1.Id,
            amount: new Money(600),
            type: TransactionType.Expense,
            paymentMethod: PaymentMethod.CreditCard,
            description: "Contas de luz, água e internet",
            date: DateTime.UtcNow.AddDays(-7),
            categoryId: categoryExpense3.Id,
            creditCardId: card1.Id
        );

        var transactionIncome1 = Transaction.Create(
            userId: user.Id,
            accountId: account1.Id,
            amount: new Money(8000),
            type: TransactionType.Income,
            paymentMethod: PaymentMethod.Account,
            description: "Salário - Empresa X",
            date: DateTime.UtcNow.AddDays(-16),
            categoryId: categoryIncome1.Id,
            creditCardId: null
        );

        
        var transactionIncome2 = Transaction.Create(
            userId: user.Id,
            accountId: account1.Id,
            amount: new Money(2000),
            type: TransactionType.Income,
            paymentMethod: PaymentMethod.Account,
            description: "Investimentos - Ações Z",
            date: DateTime.UtcNow.AddDays(-7),
            categoryId: categoryIncome3.Id,
            creditCardId: null
        );

        var transactionIncome3 = Transaction.Create(
            userId: user.Id,
            accountId: account2.Id,
            amount: new Money(950),
            type: TransactionType.Income,
            paymentMethod: PaymentMethod.Account,
            description: "Bonus de desempenho",
            date: DateTime.UtcNow.AddDays(-10),
            categoryId: categoryIncome4.Id,
            creditCardId: null
        );

        context.Transactions.Add(transactionExpense1);
        context.Transactions.Add(transactionExpense2);
        context.Transactions.Add(transactionExpense3);
        context.Transactions.Add(transactionIncome1);
        context.Transactions.Add(transactionIncome2);
        context.Transactions.Add(transactionIncome3);

        await context.SaveChangesAsync();

        return user.Id;
    }
}
