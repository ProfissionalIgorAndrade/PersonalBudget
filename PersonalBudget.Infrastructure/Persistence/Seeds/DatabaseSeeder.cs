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
            return Guid.Empty;

        var userName = Faker.Person.FullName;
        var userEmail = "email@email.com";

        var user = new User(
            name: userName,
            email: new Email(userEmail),
            passwordHash: new PasswordHasher().Hash("Email123@")
        );

        context.Users.Add(user);

        /*
         ACCOUNTS
        */

        var account1 = Account.Create(
            userId: user.Id,
            bank: Bank.Nubank,
            agency: new BankAgency("0001"),
            number: new BankAccountNumber("123456-7"),
            initialBalance: new Money(10000)
        );

        var account2 = Account.Create(
            userId: user.Id,
            bank: Bank.Itau,
            agency: new BankAgency("1301"),
            number: new BankAccountNumber("889922-3"),
            initialBalance: new Money(5000)
        );

        context.Accounts.Add(account1);
        context.Accounts.Add(account2);

        /*
         CATEGORIES
        */

        var moradia = Category.Create(user.Id, "Moradia", CategoryType.Expense);
        var alimentacao = Category.Create(user.Id, "Alimentação", CategoryType.Expense);
        var lazer = Category.Create(user.Id, "Diversão", CategoryType.Expense);

        var salario = Category.Create(user.Id, "Salário", CategoryType.Income);
        var investimento = Category.Create(user.Id, "Investimentos", CategoryType.Income);

        context.Categories.AddRange(
            moradia,
            alimentacao,
            lazer,
            salario,
            investimento
        );

        /*
         CREDIT CARDS
        */

        var nubankCard = CreditCard.Create(
            userId: user.Id,
            accountId: account1.Id,
            name: "Nubank Ultravioleta",
            limit: 8000,
            closingDay: 30,
            dueDay: 10
        );

        var itauCard = CreditCard.Create(
            userId: user.Id,
            accountId: account2.Id,
            name: "Itaú Visa Platinum",
            limit: 5000,
            closingDay: 28,
            dueDay: 6
        );

        context.CreditCards.Add(nubankCard);
        context.CreditCards.Add(itauCard);

        /*
         TRANSACTIONS
        */

        var now = DateTime.UtcNow;
        var lastMonth = now.AddMonths(-1);
        var nextMonth = now.AddMonths(1);
        var twoMonthsAhead = now.AddMonths(2);

        // Incomes (salários, investimentos) em meses diferentes
        var salaryLastMonth = Transaction.Create(
            userId: user.Id,
            accountId: account1.Id,
            amount: new Money(7800),
            type: TransactionType.Income,
            paymentMethod: PaymentMethod.Account,
            description: "Salário - Empresa X (mês passado)",
            date: new DateTime(lastMonth.Year, lastMonth.Month, 5),
            categoryId: salario.Id,
            creditCardId: null
        );

        var salaryCurrentMonth = Transaction.Create(
            userId: user.Id,
            accountId: account1.Id,
            amount: new Money(8000),
            type: TransactionType.Income,
            paymentMethod: PaymentMethod.Account,
            description: "Salário - Empresa X (mês atual)",
            date: new DateTime(now.Year, now.Month, 5),
            categoryId: salario.Id,
            creditCardId: null
        );

        var salaryNextMonth = Transaction.Create(
            userId: user.Id,
            accountId: account1.Id,
            amount: new Money(8200),
            type: TransactionType.Income,
            paymentMethod: PaymentMethod.Account,
            description: "Salário - Empresa X (próximo mês)",
            date: new DateTime(nextMonth.Year, nextMonth.Month, 5),
            categoryId: salario.Id,
            creditCardId: null
        );

        var investmentIncome = Transaction.Create(
            userId: user.Id,
            accountId: account2.Id,
            amount: new Money(350),
            type: TransactionType.Income,
            paymentMethod: PaymentMethod.Account,
            description: "Rendimento investimentos",
            date: new DateTime(now.Year, now.Month, 20),
            categoryId: investimento.Id,
            creditCardId: null
        );

        // Despesas recorrentes em conta (aluguel, contas fixas)
        var aluguelLastMonth = Transaction.Create(
            userId: user.Id,
            accountId: account1.Id,
            amount: new Money(2500),
            type: TransactionType.Expense,
            paymentMethod: PaymentMethod.Account,
            description: "Aluguel (mês passado)",
            date: new DateTime(lastMonth.Year, lastMonth.Month, 8),
            categoryId: moradia.Id,
            creditCardId: null
        );

        var aluguelCurrentMonth = Transaction.Create(
            userId: user.Id,
            accountId: account1.Id,
            amount: new Money(2500),
            type: TransactionType.Expense,
            paymentMethod: PaymentMethod.Account,
            description: "Aluguel (mês atual)",
            date: new DateTime(now.Year, now.Month, 8),
            categoryId: moradia.Id,
            creditCardId: null
        );

        var aluguelNextMonth = Transaction.Create(
            userId: user.Id,
            accountId: account1.Id,
            amount: new Money(2500),
            type: TransactionType.Expense,
            paymentMethod: PaymentMethod.Account,
            description: "Aluguel (próximo mês)",
            date: new DateTime(nextMonth.Year, nextMonth.Month, 8),
            categoryId: moradia.Id,
            creditCardId: null
        );

        var aluguelTwoMonthsAhead = Transaction.Create(
            userId: user.Id,
            accountId: account1.Id,
            amount: new Money(2500),
            type: TransactionType.Expense,
            paymentMethod: PaymentMethod.Account,
            description: "Aluguel (daqui a 2 meses)",
            date: new DateTime(twoMonthsAhead.Year, twoMonthsAhead.Month, 8),
            categoryId: moradia.Id,
            creditCardId: null
        );

        // Outras despesas em conta
        var internet = Transaction.Create(
            userId: user.Id,
            accountId: account1.Id,
            amount: new Money(150),
            type: TransactionType.Expense,
            paymentMethod: PaymentMethod.Account,
            description: "Internet banda larga",
            date: new DateTime(now.Year, now.Month, 12),
            categoryId: moradia.Id,
            creditCardId: null
        );

        var luz = Transaction.Create(
            userId: user.Id,
            accountId: account2.Id,
            amount: new Money(220),
            type: TransactionType.Expense,
            paymentMethod: PaymentMethod.Account,
            description: "Conta de luz",
            date: new DateTime(now.Year, now.Month, 15),
            categoryId: moradia.Id,
            creditCardId: null
        );

        var agua = Transaction.Create(
            userId: user.Id,
            accountId: account2.Id,
            amount: new Money(130),
            type: TransactionType.Expense,
            paymentMethod: PaymentMethod.Account,
            description: "Conta de água",
            date: new DateTime(now.Year, now.Month, 10),
            categoryId: moradia.Id,
            creditCardId: null
        );

        // Compras no cartão Nubank em diferentes meses
        var tvAmount = new Money(3200);
        var tvDate = new DateTime(now.Year, now.Month, 3);
        var tvStatement = nubankCard.AddExpense(tvDate, tvAmount);
        var tv = Transaction.Create(
            userId: user.Id,
            accountId: account1.Id,
            amount: tvAmount,
            type: TransactionType.Expense,
            paymentMethod: PaymentMethod.CreditCard,
            description: "TV OLED LG - Amazon",
            date: tvDate,
            categoryId: lazer.Id,
            creditCardId: nubankCard.Id,
            statementId: tvStatement.Id
        );

        var supermercado1Amount = new Money(430);
        var supermercado1Date = new DateTime(now.Year, now.Month, 7);
        var supermercado1Statement = nubankCard.AddExpense(supermercado1Date, supermercado1Amount);
        var supermercado1 = Transaction.Create(
            userId: user.Id,
            accountId: account1.Id,
            amount: supermercado1Amount,
            type: TransactionType.Expense,
            paymentMethod: PaymentMethod.CreditCard,
            description: "Supermercado Extra",
            date: supermercado1Date,
            categoryId: alimentacao.Id,
            creditCardId: nubankCard.Id,
            statementId: supermercado1Statement.Id
        );

        var supermercado2Amount = new Money(520);
        var supermercado2Date = new DateTime(lastMonth.Year, lastMonth.Month, 18);
        var supermercado2Statement = nubankCard.AddExpense(supermercado2Date, supermercado2Amount);
        var supermercado2 = Transaction.Create(
            userId: user.Id,
            accountId: account1.Id,
            amount: supermercado2Amount,
            type: TransactionType.Expense,
            paymentMethod: PaymentMethod.CreditCard,
            description: "Supermercado Extra (mês passado)",
            date: supermercado2Date,
            categoryId: alimentacao.Id,
            creditCardId: nubankCard.Id,
            statementId: supermercado2Statement.Id
        );

        var farmaciaAmount = new Money(210);
        var farmaciaDate = new DateTime(now.Year, now.Month, 11);
        var farmaciaStatement = nubankCard.AddExpense(farmaciaDate, farmaciaAmount);
        var farmacia = Transaction.Create(
            userId: user.Id,
            accountId: account1.Id,
            amount: farmaciaAmount,
            type: TransactionType.Expense,
            paymentMethod: PaymentMethod.CreditCard,
            description: "Farmácia Drogasil",
            date: farmaciaDate,
            categoryId: alimentacao.Id,
            creditCardId: nubankCard.Id,
            statementId: farmaciaStatement.Id
        );

        var cinemaAmount = new Money(80);
        var cinemaDate = new DateTime(now.Year, now.Month, 16);
        var cinemaStatement = nubankCard.AddExpense(cinemaDate, cinemaAmount);
        var cinema = Transaction.Create(
            userId: user.Id,
            accountId: account1.Id,
            amount: cinemaAmount,
            type: TransactionType.Expense,
            paymentMethod: PaymentMethod.CreditCard,
            description: "Cinema - Shopping",
            date: cinemaDate,
            categoryId: lazer.Id,
            creditCardId: nubankCard.Id,
            statementId: cinemaStatement.Id
        );

        var viagemFuturaAmount = new Money(1800);
        var viagemFuturaDate = new DateTime(twoMonthsAhead.Year, twoMonthsAhead.Month, 2);
        var viagemFuturaStatement = nubankCard.AddExpense(viagemFuturaDate, viagemFuturaAmount);
        var viagemFutura = Transaction.Create(
            userId: user.Id,
            accountId: account1.Id,
            amount: viagemFuturaAmount,
            type: TransactionType.Expense,
            paymentMethod: PaymentMethod.CreditCard,
            description: "Passagens aéreas (daqui a 2 meses)",
            date: viagemFuturaDate,
            categoryId: lazer.Id,
            creditCardId: nubankCard.Id,
            statementId: viagemFuturaStatement.Id
        );

        // Compras no cartão Itaú em diferentes meses
        var restauranteAmount = new Money(220);
        var restauranteDate = new DateTime(now.Year, now.Month, 9);
        var restauranteStatement = itauCard.AddExpense(restauranteDate, restauranteAmount);
        var restaurante = Transaction.Create(
            userId: user.Id,
            accountId: account2.Id,
            amount: restauranteAmount,
            type: TransactionType.Expense,
            paymentMethod: PaymentMethod.CreditCard,
            description: "Restaurante Japonês",
            date: restauranteDate,
            categoryId: lazer.Id,
            creditCardId: itauCard.Id,
            statementId: restauranteStatement.Id
        );

        var uberAmount = new Money(65);
        var uberDate = new DateTime(now.Year, now.Month, 6);
        var uberStatement = itauCard.AddExpense(uberDate, uberAmount);
        var uber = Transaction.Create(
            userId: user.Id,
            accountId: account2.Id,
            amount: uberAmount,
            type: TransactionType.Expense,
            paymentMethod: PaymentMethod.CreditCard,
            description: "Uber trabalho",
            date: uberDate,
            categoryId: lazer.Id,
            creditCardId: itauCard.Id,
            statementId: uberStatement.Id
        );

        var ifoodAmount = new Money(95);
        var ifoodDate = new DateTime(now.Year, now.Month, 14);
        var ifoodStatement = itauCard.AddExpense(ifoodDate, ifoodAmount);
        var ifood = Transaction.Create(
            userId: user.Id,
            accountId: account2.Id,
            amount: ifoodAmount,
            type: TransactionType.Expense,
            paymentMethod: PaymentMethod.CreditCard,
            description: "iFood - jantar",
            date: ifoodDate,
            categoryId: alimentacao.Id,
            creditCardId: itauCard.Id,
            statementId: ifoodStatement.Id
        );

        var combustivelAmount = new Money(260);
        var combustivelDate = new DateTime(lastMonth.Year, lastMonth.Month, 22);
        var combustivelStatement = itauCard.AddExpense(combustivelDate, combustivelAmount);
        var combustivel = Transaction.Create(
            userId: user.Id,
            accountId: account2.Id,
            amount: combustivelAmount,
            type: TransactionType.Expense,
            paymentMethod: PaymentMethod.CreditCard,
            description: "Posto de gasolina",
            date: combustivelDate,
            categoryId: lazer.Id,
            creditCardId: itauCard.Id,
            statementId: combustivelStatement.Id
        );

        var assinaturaStreamingAmount = new Money(55);
        var assinaturaStreamingDate = new DateTime(now.Year, now.Month, 1);
        var assinaturaStreamingStatement = itauCard.AddExpense(assinaturaStreamingDate, assinaturaStreamingAmount);
        var assinaturaStreaming = Transaction.Create(
            userId: user.Id,
            accountId: account2.Id,
            amount: assinaturaStreamingAmount,
            type: TransactionType.Expense,
            paymentMethod: PaymentMethod.CreditCard,
            description: "Assinatura streaming",
            date: assinaturaStreamingDate,
            categoryId: lazer.Id,
            creditCardId: itauCard.Id,
            statementId: assinaturaStreamingStatement.Id
        );

        var mercadoFuturoAmount = new Money(480);
        var mercadoFuturoDate = new DateTime(nextMonth.Year, nextMonth.Month, 4);
        var mercadoFuturoStatement = itauCard.AddExpense(mercadoFuturoDate, mercadoFuturoAmount);
        var mercadoFuturo = Transaction.Create(
            userId: user.Id,
            accountId: account2.Id,
            amount: mercadoFuturoAmount,
            type: TransactionType.Expense,
            paymentMethod: PaymentMethod.CreditCard,
            description: "Supermercado (próximo mês)",
            date: mercadoFuturoDate,
            categoryId: alimentacao.Id,
            creditCardId: itauCard.Id,
            statementId: mercadoFuturoStatement.Id
        );

        // Mais algumas despesas variadas para dar volume
        var barzinho = Transaction.Create(
            userId: user.Id,
            accountId: account1.Id,
            amount: new Money(120),
            type: TransactionType.Expense,
            paymentMethod: PaymentMethod.CreditCard,
            description: "Barzinho com amigos",
            date: new DateTime(now.Year, now.Month, 18),
            categoryId: lazer.Id,
            creditCardId: nubankCard.Id
        );

        var padaria = Transaction.Create(
            userId: user.Id,
            accountId: account1.Id,
            amount: new Money(35),
            type: TransactionType.Expense,
            paymentMethod: PaymentMethod.Account,
            description: "Padaria",
            date: new DateTime(now.Year, now.Month, 2),
            categoryId: alimentacao.Id,
            creditCardId: null
        );

        var academia = Transaction.Create(
            userId: user.Id,
            accountId: account2.Id,
            amount: new Money(120),
            type: TransactionType.Expense,
            paymentMethod: PaymentMethod.Account,
            description: "Mensalidade academia",
            date: new DateTime(now.Year, now.Month, 3),
            categoryId: lazer.Id,
            creditCardId: null
        );

        var depositoPoupanca = Transaction.Create(
            userId: user.Id,
            accountId: account1.Id,
            amount: new Money(1000),
            type: TransactionType.Expense,
            paymentMethod: PaymentMethod.Account,
            description: "Depósito poupança",
            date: new DateTime(now.Year, now.Month, 21),
            categoryId: investimento.Id,
            creditCardId: null
        );

        var resgatePoupanca = Transaction.Create(
            userId: user.Id,
            accountId: account1.Id,
            amount: new Money(500),
            type: TransactionType.Income,
            paymentMethod: PaymentMethod.Account,
            description: "Resgate poupança",
            date: new DateTime(nextMonth.Year, nextMonth.Month, 21),
            categoryId: investimento.Id,
            creditCardId: null
        );

        var viagemCurta = Transaction.Create(
            userId: user.Id,
            accountId: account2.Id,
            amount: new Money(600),
            type: TransactionType.Expense,
            paymentMethod: PaymentMethod.Account,
            description: "Viagem de fim de semana",
            date: new DateTime(lastMonth.Year, lastMonth.Month, 25),
            categoryId: lazer.Id,
            creditCardId: null
        );

        var taxiAeroportoAmount = new Money(90);
        var taxiAeroportoDate = new DateTime(twoMonthsAhead.Year, twoMonthsAhead.Month, 1);
        var taxiAeroportoStatement = itauCard.AddExpense(taxiAeroportoDate, taxiAeroportoAmount);
        var taxiAeroporto = Transaction.Create(
            userId: user.Id,
            accountId: account2.Id,
            amount: taxiAeroportoAmount,
            type: TransactionType.Expense,
            paymentMethod: PaymentMethod.CreditCard,
            description: "Táxi aeroporto",
            date: taxiAeroportoDate,
            categoryId: lazer.Id,
            creditCardId: itauCard.Id,
            statementId: taxiAeroportoStatement.Id
        );

        context.Transactions.AddRange(
            salaryLastMonth,
            salaryCurrentMonth,
            salaryNextMonth,
            investmentIncome,
            aluguelLastMonth,
            aluguelCurrentMonth,
            aluguelNextMonth,
            aluguelTwoMonthsAhead,
            internet,
            luz,
            agua,
            tv,
            supermercado1,
            supermercado2,
            farmacia,
            cinema,
            viagemFutura,
            restaurante,
            uber,
            ifood,
            combustivel,
            assinaturaStreaming,
            mercadoFuturo,
            barzinho,
            padaria,
            academia,
            depositoPoupanca,
            resgatePoupanca,
            viagemCurta,
            taxiAeroporto
        );

        await context.SaveChangesAsync();

        return user.Id;
    }
}
