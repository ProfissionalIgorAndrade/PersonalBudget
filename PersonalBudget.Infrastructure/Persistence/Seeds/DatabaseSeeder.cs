namespace PersonalBudget.Infrastructure.Persistence.Seed;

public static class DatabaseSeeder
{
    public static async Task<Guid> SeedAsync(AppDbContext context)
    {
        if (context.Users.Any())
            return Guid.Empty; // banco já populado

        // USER
        var user = new User(
            name: "Demo User",
            email: new Email("email@email.com"),
            passwordHash: new PasswordHasher().Hash("Email123@")
        );

        context.Users.Add(user);

        // ACCOUNT 1
        var account1 = Account.Create(
            userId: user.Id,
            bank: Bank.Nubank,
            agency: new BankAgency("0001"),
            number: new BankAccountNumber("123456-7"),
            initialBalance: new Money(5000)
        );

        context.Accounts.Add(account1);

        // ACCOUNT 2
        var account2 = Account.Create(
            userId: user.Id,
            bank: Bank.Santander,
            agency: new BankAgency("10101"),
            number: new BankAccountNumber("78945-7"),
            initialBalance: new Money(9000)
        );

        context.Accounts.Add(account2);

        // CATEGORY
        var categoryExpense1 = Category.Create(
            userId: user.Id,
            name: "Casa",
            type: CategoryType.Expense
        );

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

        var categoryExpenseBesteiras = Category.Create(
            userId: user.Id,
            name: "Besteiras",
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

        var categoryIncome2 = Category.Create(
                    userId: user.Id,
                    name: "Freelance",
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

        context.Categories.Add(categoryExpense1);
        context.Categories.Add(categoryExpense2);
        context.Categories.Add(categoryExpense3);
        context.Categories.Add(categoryExpenseMoradia);
        context.Categories.Add(categoryExpenseBesteiras);
        context.Categories.Add(categoryIncome1);
        context.Categories.Add(categoryIncome2);
        context.Categories.Add(categoryIncome3);
        context.Categories.Add(categoryIncome4);

        // CREDIT CARD
        var card1 = CreditCard.Create(
            userId: user.Id,
            accountId: account1.Id,
            name: "Visa Platinum",
            limit: 8000,
            closingDay: 5,
            dueDay: 12
        );

        var card2 = CreditCard.Create(
            userId: user.Id,
            accountId: account2.Id,
            name: "Smiles Premium",
            limit: 8500,
            closingDay: 15,
            dueDay: 06
        );

        var card3 = CreditCard.Create(
            userId: user.Id,
            accountId: account1.Id,
            name: "Ultravioleta Infinite",
            limit: 19000,
            closingDay: 15,
            dueDay: 19
        );

        context.CreditCards.Add(card1);
        context.CreditCards.Add(card2);
        context.CreditCards.Add(card3);

        // TRANSACTION
        var transactionExpense1 = Transaction.Create(
            userId: user.Id,
            accountId: account1.Id,
            amount: new Money(12000),
            type: TransactionType.Expense,
            paymentMethod: PaymentMethod.CreditCard,
            description: "TV Samsung 65 Polegadas 4K UHD - Magazine Luiza",
            date: DateTime.UtcNow,
            categoryId: categoryExpense1.Id,
            creditCardId: card1.Id
        );

        var transactionExpense2 = Transaction.Create(
            userId: user.Id,
            accountId: account2.Id,
            amount: new Money(2500),
            type: TransactionType.Expense,
            paymentMethod: PaymentMethod.CreditCard,
            description: "Home teatcher LG - Amazon",
            date: DateTime.UtcNow,
            categoryId: categoryExpense2.Id,
            creditCardId: card2.Id
        );

        var transactionExpense3 = Transaction.Create(
            userId: user.Id,
            accountId: account1.Id,
            amount: new Money(4000),
            type: TransactionType.Expense,
            paymentMethod: PaymentMethod.CreditCard,
            description: "Nintendo Switch - Americanas",
            date: DateTime.UtcNow,
            categoryId: categoryExpense3.Id,
            creditCardId: card3.Id
        );

        var transactionIncome1 = Transaction.Create(
            userId: user.Id,
            accountId: account1.Id,
            amount: new Money(12000),
            type: TransactionType.Income,
            paymentMethod: PaymentMethod.Account,
            description: "Salário - Empresa X",
            date: DateTime.UtcNow,
            categoryId: categoryIncome1.Id,
            creditCardId: null
        );

        
        var transactionIncome2 = Transaction.Create(
            userId: user.Id,
            accountId: account2.Id,
            amount: new Money(1500),
            type: TransactionType.Income,
            paymentMethod: PaymentMethod.Account,
            description: "Freelance - Projeto Y",
            date: DateTime.UtcNow,
            categoryId: categoryIncome2.Id,
            creditCardId: null
        );

        var transactionIncome3 = Transaction.Create(
            userId: user.Id,
            accountId: account2.Id,
            amount: new Money(950),
            type: TransactionType.Income,
            paymentMethod: PaymentMethod.Account,
            description: "Investimentos - Ações Z",
            date: DateTime.UtcNow,
            categoryId: categoryIncome3.Id,
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
