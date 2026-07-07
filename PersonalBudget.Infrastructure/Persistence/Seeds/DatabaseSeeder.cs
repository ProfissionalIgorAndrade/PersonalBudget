namespace PersonalBudget.Infrastructure.Persistence.Seed;

public static class DatabaseSeeder
{
    public static async Task<Guid> SeedAsync(AppDbContext context)
    {
        if (context.Users.Any())
            return Guid.Empty;

        var igorName = "Igor";
        var igorEmail = "email@email.com";
        var igor = new User(
            name: igorName,
            email: new Email(igorEmail),
            passwordHash: new PasswordHasher().Hash("Email123@")
        );

        var andrezaName = "Andreza";
        var andrezaEmail = "andreza@email.com";
        var andreza = new User(
            name: andrezaName,
            email: new Email(andrezaEmail),
            passwordHash: new PasswordHasher().Hash("Email123@")
        );

        context.Users.Add(igor);
        context.Users.Add(andreza);

        var household = Household.Create("Família Demo");
        context.Households.Add(household);

        var membershipIgor = HouseholdMembership.CreateOwner(household.Id, igor.Id);
        var membershipAndreza = HouseholdMembership.CreateMember(household.Id, andreza.Id);
        context.HouseholdMemberships.Add(membershipIgor);
        context.HouseholdMemberships.Add(membershipAndreza);

        var profileIgor = HouseholdMemberProfile.CreateLinkedUser(household.Id, igor.Id, igorName, 0);
        var profileAndreza = HouseholdMemberProfile.CreateLinkedUser(household.Id, andreza.Id, andrezaName, 1);
        var profileFamilia = HouseholdMemberProfile.CreateJoint("Família", household.Id, 2);
        context.HouseholdMemberProfiles.Add(profileIgor);
        context.HouseholdMemberProfiles.Add(profileAndreza);
        context.HouseholdMemberProfiles.Add(profileFamilia);

        await context.SaveChangesAsync();

        var hId = household.Id;

        var account1 = Account.Create(
            userId: igor.Id,
            householdId: hId,
            bank: Bank.Nubank,
            agency: new BankAgency("0001"),
            number: new BankAccountNumber("123456-7"),
            initialBalance: new Money(10000)
        );

        var account2 = Account.Create(
            userId: igor.Id,
            householdId: hId,
            bank: Bank.Itau,
            agency: new BankAgency("1301"),
            number: new BankAccountNumber("889922-3"),
            initialBalance: new Money(5000)
        );

        var accountConjunta = Account.Create(
            userId: andreza.Id,
            householdId: hId,
            bank: Bank.Inter,
            agency: new BankAgency("0001"),
            number: new BankAccountNumber("98765-0"),
            initialBalance: new Money(12000)
        );

        context.Accounts.Add(account1);
        context.Accounts.Add(account2);
        context.Accounts.Add(accountConjunta);

        var moradia = Category.Create(hId, "Moradia", CategoryType.Expense);
        var alimentacao = Category.Create(hId, "Alimentação", CategoryType.Expense);
        var lazer = Category.Create(hId, "Diversão", CategoryType.Expense);
        var transporte = Category.Create(hId, "Transporte", CategoryType.Expense);
        var saude = Category.Create(hId, "Saúde", CategoryType.Expense);
        var educacao = Category.Create(hId, "Educação", CategoryType.Expense);
        var presentes = Category.Create(hId, "Presentes", CategoryType.Expense);
        var servicos = Category.Create(hId, "Serviços & Assinaturas", CategoryType.Expense);
        var salario = Category.Create(hId, "Salário", CategoryType.Income);
        var investimento = Category.Create(hId, "Investimentos", CategoryType.Income);
        var freelance = Category.Create(hId, "Freelance / Bicos", CategoryType.Income);
        var bonus = Category.Create(hId, "Bônus & 13º", CategoryType.Income);
        var cashback = Category.Create(hId, "Cashback & Reembolsos", CategoryType.Income);

        context.Categories.AddRange(
            moradia,
            alimentacao,
            lazer,
            transporte,
            saude,
            educacao,
            presentes,
            servicos,
            salario,
            investimento,
            freelance,
            bonus,
            cashback
        );

        var nubankCard = CreditCard.Create(
            userId: igor.Id,
            householdId: hId,
            accountId: account1.Id,
            name: "Nubank Ultravioleta",
            limit: 8000,
            closingDay: 30,
            dueDay: 10
        );

        var itauCard = CreditCard.Create(
            userId: igor.Id,
            householdId: hId,
            accountId: account2.Id,
            name: "Itaú Visa Platinum",
            limit: 5000,
            closingDay: 28,
            dueDay: 6
        );

        context.CreditCards.Add(nubankCard);
        context.CreditCards.Add(itauCard);

        Console.WriteLine("CreditCards: " + itauCard.Id);
        Console.WriteLine("CreditCards: " + nubankCard.Id);

        // Não salvar cartões antes de AddExpense: as faturas vão para o campo privado _statements e,
        // se o cartão já estiver persistido (Unchanged), o provider InMemory pode gerar DbUpdateConcurrencyException
        // ao tentar atualizar faturas que o modelo não rastreou corretamente. Tudo segue num único SaveChanges ao final.

        var now = DateTime.UtcNow;
        var lastMonth = now.AddMonths(-1);
        var nextMonth = now.AddMonths(1);
        var twoMonthsAhead = now.AddMonths(2);

        Guid PIG = profileIgor.Id;
        Guid PAN = profileAndreza.Id;
        Guid PFAM = profileFamilia.Id;

        var transactions = new List<Transaction>();
        var rng = new Random(20250318);

        Transaction AddTx(Transaction t) { transactions.Add(t); return t; }

        Account AccountForCard(CreditCard card)
            => card.AccountId == account1.Id ? account1 : card.AccountId == account2.Id ? account2 : accountConjunta;

        Transaction AddCcExpense(
            Guid userId,
            Guid profileId,
            CreditCard card,
            Money amount,
            DateTime date,
            string description,
            Guid categoryId,
            TransactionFrequency frequency = TransactionFrequency.Variable)
        {
            var statement = card.AddExpense(date, amount);
            return AddTx(Transaction.Create(
                userId,
                hId,
                profileId,
                AccountForCard(card).Id,
                amount,
                TransactionType.Expense,
                PaymentMethod.CreditCard,
                date,
                description,
                categoryId,
                card.Id,
                statement.Id,
                frequency: frequency,
                dueDate: frequency == TransactionFrequency.Fixed ? date.Date : null));
        }

        void AddTransferPair(Guid userId, Guid profileId, Account from, Account to, decimal amount, DateTime date, string description)
        {
            var transferId = Guid.NewGuid();
            AddTx(Transaction.Create(
                userId,
                hId,
                profileId,
                from.Id,
                new Money(amount),
                TransactionType.Expense,
                PaymentMethod.Transfer,
                date,
                $"Transferência: {description} (saída)",
                categoryId: null,
                transferId: transferId,
                frequency: TransactionFrequency.Variable));
            AddTx(Transaction.Create(
                userId,
                hId,
                profileId,
                to.Id,
                new Money(amount),
                TransactionType.Income,
                PaymentMethod.Transfer,
                date,
                $"Transferência: {description} (entrada)",
                categoryId: null,
                transferId: transferId,
                frequency: TransactionFrequency.Variable));
        }

        static DateTime RandomDayInRange(Random random, DateTime start, DateTime end)
        {
            var days = Math.Max(0, (end.Date - start.Date).Days);
            return start.AddDays(random.Next(days + 1));
        }

        // --- Salário Empresa X (série recorrente: mês passado / atual / próximo) ---
        var salarioIgorRecId = Guid.NewGuid();
        AddTx(Transaction.Create(igor.Id, hId, PIG, account1.Id, new Money(7800), TransactionType.Income, PaymentMethod.Account,
            new DateTime(lastMonth.Year, lastMonth.Month, 5), "Salário - Empresa X (mês passado)", salario.Id, frequency: TransactionFrequency.Fixed, dueDate: new DateTime(lastMonth.Year, lastMonth.Month, 5)))
            .AssignRecurrenceId(salarioIgorRecId);
        AddTx(Transaction.Create(igor.Id, hId, PIG, account1.Id, new Money(8000), TransactionType.Income, PaymentMethod.Account,
            new DateTime(now.Year, now.Month, 5), "Salário - Empresa X (mês atual)", salario.Id, frequency: TransactionFrequency.Fixed, dueDate: new DateTime(now.Year, now.Month, 5)))
            .AssignRecurrenceId(salarioIgorRecId);
        AddTx(Transaction.Create(igor.Id, hId, PAN, account1.Id, new Money(8200), TransactionType.Income, PaymentMethod.Account,
            new DateTime(nextMonth.Year, nextMonth.Month, 5), "Salário - Empresa X (próximo mês)", salario.Id, frequency: TransactionFrequency.Fixed, dueDate: new DateTime(nextMonth.Year, nextMonth.Month, 5)))
            .AssignRecurrenceId(salarioIgorRecId);

        AddTx(Transaction.Create(igor.Id, hId, PIG, account2.Id, new Money(350), TransactionType.Income, PaymentMethod.Account,
            new DateTime(now.Year, now.Month, 20), "Rendimento investimentos", investimento.Id));

        // --- Aluguel (série recorrente: mês passado / atual / próximo / daqui a 2 meses) ---
        var aluguelRecId = Guid.NewGuid();
        AddTx(Transaction.Create(igor.Id, hId, PFAM, account1.Id, new Money(2500), TransactionType.Expense, PaymentMethod.Account,
            new DateTime(lastMonth.Year, lastMonth.Month, 8), "Aluguel (mês passado)", moradia.Id, frequency: TransactionFrequency.Fixed, dueDate: new DateTime(lastMonth.Year, lastMonth.Month, 8)))
            .AssignRecurrenceId(aluguelRecId);
        AddTx(Transaction.Create(igor.Id, hId, PFAM, account1.Id, new Money(2500), TransactionType.Expense, PaymentMethod.Account,
            new DateTime(now.Year, now.Month, 8), "Aluguel (mês atual)", moradia.Id, frequency: TransactionFrequency.Fixed, dueDate: new DateTime(now.Year, now.Month, 8)))
            .AssignRecurrenceId(aluguelRecId);
        AddTx(Transaction.Create(igor.Id, hId, PFAM, account1.Id, new Money(2500), TransactionType.Expense, PaymentMethod.Account,
            new DateTime(nextMonth.Year, nextMonth.Month, 8), "Aluguel (próximo mês)", moradia.Id, frequency: TransactionFrequency.Fixed, dueDate: new DateTime(nextMonth.Year, nextMonth.Month, 8)))
            .AssignRecurrenceId(aluguelRecId);
        AddTx(Transaction.Create(igor.Id, hId, PFAM, account1.Id, new Money(2500), TransactionType.Expense, PaymentMethod.Account,
            new DateTime(twoMonthsAhead.Year, twoMonthsAhead.Month, 8), "Aluguel (daqui a 2 meses)", moradia.Id, frequency: TransactionFrequency.Fixed, dueDate: new DateTime(twoMonthsAhead.Year, twoMonthsAhead.Month, 8)))
            .AssignRecurrenceId(aluguelRecId);

        // --- Contas fixas mensais (Fixed isolados — cada uma é sua própria série no seed) ---
        AddTx(Transaction.Create(igor.Id, hId, PIG, account1.Id, new Money(150), TransactionType.Expense, PaymentMethod.Account,
            new DateTime(now.Year, now.Month, 12), "Internet banda larga", moradia.Id, frequency: TransactionFrequency.Fixed, dueDate: new DateTime(now.Year, now.Month, 12)))
            .AssignRecurrenceId(Guid.NewGuid());
        AddTx(Transaction.Create(igor.Id, hId, PAN, account2.Id, new Money(220), TransactionType.Expense, PaymentMethod.Account,
            new DateTime(now.Year, now.Month, 15), "Conta de luz", moradia.Id, frequency: TransactionFrequency.Fixed, dueDate: new DateTime(now.Year, now.Month, 15)))
            .AssignRecurrenceId(Guid.NewGuid());
        AddTx(Transaction.Create(andreza.Id, hId, PAN, accountConjunta.Id, new Money(130), TransactionType.Expense, PaymentMethod.Account,
            new DateTime(now.Year, now.Month, 10), "Conta de água", moradia.Id, frequency: TransactionFrequency.Fixed, dueDate: new DateTime(now.Year, now.Month, 10)))
            .AssignRecurrenceId(Guid.NewGuid());

        var tvAmount = new Money(3200);
        var tvDate = new DateTime(now.Year, now.Month, 3);
        var tvStatement = nubankCard.AddExpense(tvDate, tvAmount);
        AddTx(Transaction.Create(igor.Id, hId, PFAM, account1.Id, tvAmount, TransactionType.Expense, PaymentMethod.CreditCard,
            tvDate, "TV OLED LG - Amazon", lazer.Id, nubankCard.Id, tvStatement.Id));

        var s1Amt = new Money(430);
        var s1Date = new DateTime(now.Year, now.Month, 7);
        var s1St = nubankCard.AddExpense(s1Date, s1Amt);
        AddTx(Transaction.Create(igor.Id, hId, PAN, account1.Id, s1Amt, TransactionType.Expense, PaymentMethod.CreditCard,
            s1Date, "Supermercado Extra", alimentacao.Id, nubankCard.Id, s1St.Id));

        var s2Amt = new Money(520);
        var s2Date = new DateTime(lastMonth.Year, lastMonth.Month, 18);
        var s2St = nubankCard.AddExpense(s2Date, s2Amt);
        AddTx(Transaction.Create(igor.Id, hId, PIG, account1.Id, s2Amt, TransactionType.Expense, PaymentMethod.CreditCard,
            s2Date, "Supermercado Extra (mês passado)", alimentacao.Id, nubankCard.Id, s2St.Id));

        var farmAmt = new Money(210);
        var farmDate = new DateTime(now.Year, now.Month, 11);
        var farmSt = nubankCard.AddExpense(farmDate, farmAmt);
        AddTx(Transaction.Create(andreza.Id, hId, PAN, account1.Id, farmAmt, TransactionType.Expense, PaymentMethod.CreditCard,
            farmDate, "Farmácia Drogasil", alimentacao.Id, nubankCard.Id, farmSt.Id));

        var cinemaAmt = new Money(80);
        var cinemaDate = new DateTime(now.Year, now.Month, 16);
        var cinemaSt = nubankCard.AddExpense(cinemaDate, cinemaAmt);
        AddTx(Transaction.Create(igor.Id, hId, PIG, account1.Id, cinemaAmt, TransactionType.Expense, PaymentMethod.CreditCard,
            cinemaDate, "Cinema - Shopping", lazer.Id, nubankCard.Id, cinemaSt.Id));

        var viaAmt = new Money(1800);
        var viaDate = new DateTime(twoMonthsAhead.Year, twoMonthsAhead.Month, 2);
        var viaSt = nubankCard.AddExpense(viaDate, viaAmt);
        AddTx(Transaction.Create(igor.Id, hId, PFAM, account1.Id, viaAmt, TransactionType.Expense, PaymentMethod.CreditCard,
            viaDate, "Passagens aéreas (daqui a 2 meses)", lazer.Id, nubankCard.Id, viaSt.Id));

        var restAmt = new Money(220);
        var restDate = new DateTime(now.Year, now.Month, 9);
        var restSt = itauCard.AddExpense(restDate, restAmt);
        AddTx(Transaction.Create(igor.Id, hId, PAN, account2.Id, restAmt, TransactionType.Expense, PaymentMethod.CreditCard,
            restDate, "Restaurante Japonês", lazer.Id, itauCard.Id, restSt.Id));

        var uberAmt = new Money(65);
        var uberDate = new DateTime(now.Year, now.Month, 6);
        var uberSt = itauCard.AddExpense(uberDate, uberAmt);
        AddTx(Transaction.Create(andreza.Id, hId, PIG, account2.Id, uberAmt, TransactionType.Expense, PaymentMethod.CreditCard,
            uberDate, "Uber trabalho", lazer.Id, itauCard.Id, uberSt.Id));

        var ifoodAmt = new Money(95);
        var ifoodDate = new DateTime(now.Year, now.Month, 14);
        var ifoodSt = itauCard.AddExpense(ifoodDate, ifoodAmt);
        AddTx(Transaction.Create(igor.Id, hId, PIG, account2.Id, ifoodAmt, TransactionType.Expense, PaymentMethod.CreditCard,
            ifoodDate, "iFood - jantar", alimentacao.Id, itauCard.Id, ifoodSt.Id));

        var combAmt = new Money(260);
        var combDate = new DateTime(lastMonth.Year, lastMonth.Month, 22);
        var combSt = itauCard.AddExpense(combDate, combAmt);
        AddTx(Transaction.Create(igor.Id, hId, PIG, account2.Id, combAmt, TransactionType.Expense, PaymentMethod.CreditCard,
            combDate, "Posto de gasolina", lazer.Id, itauCard.Id, combSt.Id));

        var streamAmt = new Money(55);
        var streamDate = new DateTime(now.Year, now.Month, 1);
        var streamSt = itauCard.AddExpense(streamDate, streamAmt);
        AddTx(Transaction.Create(igor.Id, hId, PFAM, account2.Id, streamAmt, TransactionType.Expense, PaymentMethod.CreditCard,
            streamDate, "Assinatura streaming", lazer.Id, itauCard.Id, streamSt.Id));

        var mercFutAmt = new Money(480);
        var mercFutDate = new DateTime(nextMonth.Year, nextMonth.Month, 4);
        var mercFutSt = itauCard.AddExpense(mercFutDate, mercFutAmt);
        AddTx(Transaction.Create(igor.Id, hId, PFAM, account2.Id, mercFutAmt, TransactionType.Expense, PaymentMethod.CreditCard,
            mercFutDate, "Supermercado (próximo mês)", alimentacao.Id, itauCard.Id, mercFutSt.Id));

        var barAmt = new Money(120);
        var barDate = new DateTime(now.Year, now.Month, 18);
        var barSt = nubankCard.AddExpense(barDate, barAmt);
        AddTx(Transaction.Create(igor.Id, hId, PIG, account1.Id, barAmt, TransactionType.Expense, PaymentMethod.CreditCard,
            barDate, "Barzinho com amigos", lazer.Id, nubankCard.Id, barSt.Id));

        AddTx(Transaction.Create(igor.Id, hId, PAN, account1.Id, new Money(35), TransactionType.Expense, PaymentMethod.Account,
            new DateTime(now.Year, now.Month, 2), "Padaria", alimentacao.Id));
        AddTx(Transaction.Create(andreza.Id, hId, PAN, account2.Id, new Money(120), TransactionType.Expense, PaymentMethod.Account,
            new DateTime(now.Year, now.Month, 3), "Mensalidade academia", lazer.Id, frequency: TransactionFrequency.Fixed, dueDate: new DateTime(now.Year, now.Month, 3)))
            .AssignRecurrenceId(Guid.NewGuid());
        AddTx(Transaction.Create(igor.Id, hId, PIG, account1.Id, new Money(1000), TransactionType.Expense, PaymentMethod.Account,
            new DateTime(now.Year, now.Month, 21), "Depósito poupança", investimento.Id));
        AddTx(Transaction.Create(igor.Id, hId, PIG, account1.Id, new Money(500), TransactionType.Income, PaymentMethod.Account,
            new DateTime(nextMonth.Year, nextMonth.Month, 21), "Resgate poupança", investimento.Id));
        AddTx(Transaction.Create(igor.Id, hId, PFAM, account2.Id, new Money(600), TransactionType.Expense, PaymentMethod.Account,
            new DateTime(lastMonth.Year, lastMonth.Month, 25), "Viagem de fim de semana", lazer.Id));

        var taxiAmt = new Money(90);
        var taxiDate = new DateTime(twoMonthsAhead.Year, twoMonthsAhead.Month, 1);
        var taxiSt = itauCard.AddExpense(taxiDate, taxiAmt);
        AddTx(Transaction.Create(igor.Id, hId, PIG, account2.Id, taxiAmt, TransactionType.Expense, PaymentMethod.CreditCard,
            taxiDate, "Táxi aeroporto", lazer.Id, itauCard.Id, taxiSt.Id));

        AddTx(Transaction.Create(andreza.Id, hId, PAN, accountConjunta.Id, new Money(450), TransactionType.Expense, PaymentMethod.Account,
            new DateTime(now.Year, now.Month, 22), "Compras mês - conta conjunta", alimentacao.Id));
        AddTx(Transaction.Create(igor.Id, hId, PFAM, accountConjunta.Id, new Money(199), TransactionType.Expense, PaymentMethod.Account,
            new DateTime(now.Year, now.Month, 24), "Assinatura familiar (streaming + nuvem)", lazer.Id));
        AddTx(Transaction.Create(andreza.Id, hId, PIG, accountConjunta.Id, new Money(89), TransactionType.Expense, PaymentMethod.Account,
            new DateTime(now.Year, now.Month, 25), "Presente - aniversário sogro", lazer.Id));

        // --- Demo: muitos lançamentos pseudo-aleatórios (Igor, Andreza, Família) ---
        // Cobre: Conta, Cartão, Dinheiro, Transferência; receitas e despesas; variável / fixa; parcelas no cartão.
        var rangeStart = lastMonth.AddMonths(-3);
        var rangeEnd = nextMonth.AddMonths(2);

        Guid[] expenseCatIds =
        [
            moradia.Id, alimentacao.Id, lazer.Id, transporte.Id, saude.Id, educacao.Id, presentes.Id, servicos.Id
        ];
        Guid[] incomeCatIds =
        [
            salario.Id, investimento.Id, freelance.Id, bonus.Id, cashback.Id
        ];

        string[] incomeDescs =
        [
            "Salário CLT", "Freelance — projeto web", "Consultoria pontual", "Bônus trimestral", "13º salário (antecipação)",
            "Rendimento CDB", "Dividendos FIIs", "Cashback Nubank", "Cashback cartão", "Reembolso VR", "Venda OLX",
            "Aula particular online", "Gratificação natalina", "Participação nos lucros", "Resgate Tesouro"
        ];

        string[] accountExpDescs =
        [
            "PIX mercado", "Pagamento conta luz", "Débito automático seguro", "Manutenção bicicleta", "Livros e revistas",
            "Presente escola", "Doação", "Pet shop", "Cabeleireiro", "Manicure", "Uber (PIX)", "Conta de gás",
            "Material de limpeza", "Reparo celular", "Oficina mecânica"
        ];

        string[] cashDescs =
        [
            "Feira livre (dinheiro)", "Padaria", "Sorvete na praça", "Ingresso show (cash)", "Gorjeta", "Taxa de estacionamento",
            "Brechó", "Açaí", "Pastel de feira", "Churrasquinho"
        ];

        string[] ccDescs =
        [
            "Amazon", "Mercado Livre", "Magazine Luiza", "Netshoes", "Shopee", "AliExpress", "Steam", "PlayStation Plus",
            "Spotify", "Uber", "99", "Rappi", "Zé Delivery", "Farmácia", "Ótica", "Livraria", "Decathlon"
        ];

        for (var i = 0; i < 160; i++)
        {
            var roll = rng.Next(100);
            var date = RandomDayInRange(rng, rangeStart, rangeEnd);
            var profileRoll = rng.Next(3);
            var (profileId, registrarUserId) = profileRoll switch
            {
                0 => (PIG, igor.Id),
                1 => (PAN, andreza.Id),
                _ => (PFAM, rng.Next(2) == 0 ? igor.Id : andreza.Id)
            };

            if (roll < 52)
            {
                var catId = expenseCatIds[rng.Next(expenseCatIds.Length)];
                var amt = Math.Round((decimal)rng.Next(12, 1200) + (decimal)rng.NextDouble(), 2);
                var sub = rng.Next(100);

                if (sub < 38)
                {
                    var acc = rng.Next(3) switch { 0 => account1, 1 => account2, _ => accountConjunta };
                    var freq = rng.Next(15) == 0 ? TransactionFrequency.Fixed : TransactionFrequency.Variable;
                    var desc = $"{accountExpDescs[rng.Next(accountExpDescs.Length)]} · #{i + 1}";
                    var t = AddTx(Transaction.Create(registrarUserId, hId, profileId, acc.Id, new Money(amt), TransactionType.Expense,
                        PaymentMethod.Account, date, desc, catId, frequency: freq,
                        dueDate: freq == TransactionFrequency.Fixed ? date.Date : null));
                    if (freq == TransactionFrequency.Fixed)
                        t.AssignRecurrenceId(Guid.NewGuid());
                }
                else if (sub < 78)
                {
                    var card = rng.Next(2) == 0 ? nubankCard : itauCard;
                    var desc = $"{ccDescs[rng.Next(ccDescs.Length)]} · #{i + 1}";
                    AddCcExpense(registrarUserId, profileId, card, new Money(amt), date, desc, catId);
                }
                else
                {
                    var acc = rng.Next(3) switch { 0 => account1, 1 => account2, _ => accountConjunta };
                    var desc = $"{cashDescs[rng.Next(cashDescs.Length)]} · #{i + 1}";
                    AddTx(Transaction.Create(registrarUserId, hId, profileId, acc.Id, new Money(amt), TransactionType.Expense,
                        PaymentMethod.Cash, date, desc, catId));
                }
            }
            else if (roll < 82)
            {
                var catId = incomeCatIds[rng.Next(incomeCatIds.Length)];
                var amt = Math.Round((decimal)rng.Next(200, 9500) + (decimal)rng.NextDouble(), 2);
                var acc = rng.Next(3) switch { 0 => account1, 1 => account2, _ => accountConjunta };
                var desc = $"{incomeDescs[rng.Next(incomeDescs.Length)]} · #{i + 1}";
                var freq = rng.Next(20) == 0 ? TransactionFrequency.Fixed : TransactionFrequency.Variable;
                var t = AddTx(Transaction.Create(registrarUserId, hId, profileId, acc.Id, new Money(amt), TransactionType.Income,
                    PaymentMethod.Account, date, desc, catId, frequency: freq,
                    dueDate: freq == TransactionFrequency.Fixed ? date.Date : null));
                if (freq == TransactionFrequency.Fixed)
                    t.AssignRecurrenceId(Guid.NewGuid());
            }
            else
            {
                var accFrom = rng.Next(3) switch { 0 => account1, 1 => account2, _ => accountConjunta };
                Account accTo;
                do
                {
                    accTo = rng.Next(3) switch { 0 => account1, 1 => account2, _ => accountConjunta };
                } while (accTo.Id == accFrom.Id);

                var amt = Math.Round((decimal)rng.Next(80, 5000) + (decimal)rng.NextDouble(), 2);
                var names = new[] { "Reserva emergência", "Poupança", "Investir", "Pagar cartão", "Ajuste saldo", "Casa conjunta" };
                AddTransferPair(registrarUserId, profileId, accFrom, accTo, amt, date, $"{names[rng.Next(names.Length)]} · #{i + 1}");
            }
        }

        // --- Parcelas no cartão (TransactionFrequency.Installments) — cada série com um RecurrenceId próprio ---

        {
            var totalGeladeira = 4299.90m;
            var parcelas = 10;
            var baseD = new DateTime(lastMonth.Year, lastMonth.Month, 12);
            var valorParcela = Math.Round(totalGeladeira / parcelas, 2);
            var ultima = totalGeladeira - valorParcela * (parcelas - 1);
            var recId = Guid.NewGuid();
            for (var p = 0; p < parcelas; p++)
            {
                var d = baseD.AddMonths(p);
                var v = p == parcelas - 1 ? ultima : valorParcela;
                AddCcExpense(igor.Id, PFAM, nubankCard, new Money(v), d,
                    $"Geladeira Brastemp ({p + 1}/{parcelas})", moradia.Id, TransactionFrequency.Installments)
                    .AssignRecurrenceId(recId);
            }
        }

        {
            var totalNotebook = 6999m;
            var parcelas = 12;
            var inicio = lastMonth.AddMonths(-1);
            var baseD = new DateTime(inicio.Year, inicio.Month, 5);
            var valorParcela = Math.Round(totalNotebook / parcelas, 2);
            var ultima = totalNotebook - valorParcela * (parcelas - 1);
            var recId = Guid.NewGuid();
            for (var p = 0; p < parcelas; p++)
            {
                var d = baseD.AddMonths(p);
                var v = p == parcelas - 1 ? ultima : valorParcela;
                AddCcExpense(andreza.Id, PAN, itauCard, new Money(v), d,
                    $"Notebook trabalho ({p + 1}/{parcelas})", educacao.Id, TransactionFrequency.Installments)
                    .AssignRecurrenceId(recId);
            }
        }

        {
            var totalSofa = 2400m;
            var parcelas = 6;
            var baseD = new DateTime(now.Year, now.Month, 8);
            var valorParcela = Math.Round(totalSofa / parcelas, 2);
            var ultima = totalSofa - valorParcela * (parcelas - 1);
            var recId = Guid.NewGuid();
            for (var p = 0; p < parcelas; p++)
            {
                var d = baseD.AddMonths(p);
                var v = p == parcelas - 1 ? ultima : valorParcela;
                AddCcExpense(andreza.Id, PAN, itauCard, new Money(v), d,
                    $"Sofá sala ({p + 1}/{parcelas})", moradia.Id, TransactionFrequency.Installments)
                    .AssignRecurrenceId(recId);
            }
        }

        {
            var totalCurso = 1596m;
            var parcelas = 4;
            var baseD = new DateTime(lastMonth.Year, lastMonth.Month, 20);
            var valorParcela = Math.Round(totalCurso / parcelas, 2);
            var ultima = totalCurso - valorParcela * (parcelas - 1);
            var recId = Guid.NewGuid();
            for (var p = 0; p < parcelas; p++)
            {
                var d = baseD.AddMonths(p);
                var v = p == parcelas - 1 ? ultima : valorParcela;
                AddCcExpense(igor.Id, PIG, nubankCard, new Money(v), d,
                    $"Curso online certificação ({p + 1}/{parcelas})", educacao.Id, TransactionFrequency.Installments)
                    .AssignRecurrenceId(recId);
            }
        }

        AddTransferPair(igor.Id, PIG, account1, accountConjunta, 1200, new DateTime(now.Year, now.Month, 4), "Aporte conta conjunta");
        AddTransferPair(andreza.Id, PAN, accountConjunta, account2, 850, new DateTime(now.Year, now.Month, 4), "Pagar fatura Itaú");
        AddTransferPair(igor.Id, PFAM, account2, account1, 3000, new DateTime(lastMonth.Year, lastMonth.Month, 19), "Consolidação saldo");

        // --- Mês calendário atual: volume garantido para PAN / PFAM (resumo por pessoa no front filtra mês/ano) ---
        // O gerador aleatório espalha datas em vários meses; aqui tudo cai no mesmo mês de `now` (ex.: março).
        {
            var y = now.Year;
            var mo = now.Month;
            DateTime D(int day)
            {
                var max = DateTime.DaysInMonth(y, mo);
                return new DateTime(y, mo, Math.Clamp(day, 1, max));
            }

            // Andreza (PAN) — ganhos e gastos no mês
            AddTx(Transaction.Create(andreza.Id, hId, PAN, accountConjunta.Id, new Money(6200), TransactionType.Income, PaymentMethod.Account,
                D(5), "Salário CLT — Andreza", salario.Id, frequency: TransactionFrequency.Fixed, dueDate: D(5)))
                .AssignRecurrenceId(Guid.NewGuid());
            AddTx(Transaction.Create(andreza.Id, hId, PAN, accountConjunta.Id, new Money(1200), TransactionType.Income, PaymentMethod.Account,
                D(12), "Freelance — projeto UX", freelance.Id));
            AddTx(Transaction.Create(andreza.Id, hId, PAN, accountConjunta.Id, new Money(350), TransactionType.Income, PaymentMethod.Account,
                D(20), "Bônus desempenho", bonus.Id));
            AddTx(Transaction.Create(andreza.Id, hId, PAN, accountConjunta.Id, new Money(120), TransactionType.Income, PaymentMethod.Account,
                D(25), "Cashback e reembolsos", cashback.Id));
            AddTx(Transaction.Create(andreza.Id, hId, PAN, accountConjunta.Id, new Money(540), TransactionType.Expense, PaymentMethod.Account,
                D(3), "Supermercado — Andreza", alimentacao.Id));
            AddTx(Transaction.Create(andreza.Id, hId, PAN, account2.Id, new Money(180), TransactionType.Expense, PaymentMethod.Account,
                D(6), "Combustível", transporte.Id));
            AddTx(Transaction.Create(andreza.Id, hId, PAN, accountConjunta.Id, new Money(95), TransactionType.Expense, PaymentMethod.Cash,
                D(8), "Farmácia (dinheiro)", saude.Id));
            AddTx(Transaction.Create(andreza.Id, hId, PAN, account1.Id, new Money(220), TransactionType.Expense, PaymentMethod.Account,
                D(9), "Academia", lazer.Id, frequency: TransactionFrequency.Fixed, dueDate: D(9)))
                .AssignRecurrenceId(Guid.NewGuid());
            AddCcExpense(andreza.Id, PAN, nubankCard, new Money(167.50m), D(11), "Livraria & papelaria", educacao.Id);
            AddCcExpense(andreza.Id, PAN, itauCard, new Money(289), D(14), "Restaurante — jantar", lazer.Id);
            AddCcExpense(andreza.Id, PAN, nubankCard, new Money(79.90m), D(16), "Streaming", servicos.Id);
            AddTransferPair(andreza.Id, PAN, accountConjunta, account1, 450, D(18), "Organizar saldo entre contas");
            AddTx(Transaction.Create(andreza.Id, hId, PAN, accountConjunta.Id, new Money(160), TransactionType.Expense, PaymentMethod.Account,
                D(21), "Presente — amiga secreta", presentes.Id));
            AddTx(Transaction.Create(andreza.Id, hId, PAN, account2.Id, new Money(65), TransactionType.Expense, PaymentMethod.Cash,
                D(23), "Padaria da esquina", alimentacao.Id));
            AddCcExpense(andreza.Id, PAN, itauCard, new Money(410), D(26), "Compras online — casa", moradia.Id);

            // Família (PFAM) — ganhos e gastos compartilhados no mês
            AddTx(Transaction.Create(igor.Id, hId, PFAM, accountConjunta.Id, new Money(800), TransactionType.Income, PaymentMethod.Account,
                D(2), "Venda garagem — renda extra", freelance.Id));
            AddTx(Transaction.Create(andreza.Id, hId, PFAM, account1.Id, new Money(120), TransactionType.Income, PaymentMethod.Account,
                D(7), "Cashback Nubank — família", cashback.Id));
            AddTx(Transaction.Create(igor.Id, hId, PFAM, accountConjunta.Id, new Money(1350), TransactionType.Expense, PaymentMethod.Account,
                D(4), "Supermercado — compra família", alimentacao.Id));
            AddTx(Transaction.Create(andreza.Id, hId, PFAM, account1.Id, new Money(280), TransactionType.Expense, PaymentMethod.Account,
                D(10), "Cinema — pipoca e ingressos", lazer.Id));
            AddTx(Transaction.Create(igor.Id, hId, PFAM, accountConjunta.Id, new Money(190), TransactionType.Expense, PaymentMethod.Account,
                D(13), "Conta de gás (rateio)", moradia.Id));
            AddCcExpense(igor.Id, PFAM, nubankCard, new Money(620), D(15), "Manutenção / ferramentas", moradia.Id);
            AddCcExpense(andreza.Id, PFAM, itauCard, new Money(175), D(17), "Pet shop — ração", alimentacao.Id);
            AddTransferPair(igor.Id, PFAM, account1, accountConjunta, 2000, D(19), "Aporte reserva emergência");
            AddTx(Transaction.Create(andreza.Id, hId, PFAM, account2.Id, new Money(95), TransactionType.Expense, PaymentMethod.Cash,
                D(22), "Feira — hortifruti", alimentacao.Id));
            AddCcExpense(igor.Id, PFAM, nubankCard, new Money(340), D(24), "Presente aniversário avó", presentes.Id);
            AddTx(Transaction.Create(igor.Id, hId, PFAM, accountConjunta.Id, new Money(420), TransactionType.Expense, PaymentMethod.Account,
                D(27), "Plano saúde — coparticipação", saude.Id));

            // Igor (PIG) — alguns gastos no mesmo mês (o front mostrava só ganhos)
            AddTx(Transaction.Create(igor.Id, hId, PIG, account1.Id, new Money(180), TransactionType.Expense, PaymentMethod.Account,
                D(6), "Almoço trabalho (PIX)", alimentacao.Id));
            AddCcExpense(igor.Id, PIG, itauCard, new Money(92.50m), D(12), "Café & lanche", alimentacao.Id);
            AddTx(Transaction.Create(igor.Id, hId, PIG, account2.Id, new Money(45), TransactionType.Expense, PaymentMethod.Cash,
                D(20), "Estacionamento", transporte.Id));
        }

        context.Transactions.AddRange(transactions);

        await context.SaveChangesAsync();

        return igor.Id;
    }
}
