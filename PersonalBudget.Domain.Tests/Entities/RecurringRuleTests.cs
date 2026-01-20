using FluentAssertions;

public class RecurringRuleTests
    {
        // =========================
        // HELPERS
        // =========================

        private RecurringRule CreateValidRecurringRule(
            RecurrenceFrequency frequency = RecurrenceFrequency.Monthly,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            return new RecurringRule(
                accountId: Guid.NewGuid(),
                categoryId: Guid.NewGuid(),
                amount: new Money(100),
                type: TransactionType.Expense,
                paymentMethod: PaymentMethod.Pix,
                frequency: frequency,
                startDate: startDate ?? new DateTime(2025, 1, 1),
                description: "Despesa recorrente",
                endDate: endDate
            );
        }

        // =========================
        // CONSTRUCTION INVARIANTS
        // =========================

        [Fact]
        public void RecurringRule_Should_Have_Valid_Identity()
        {
            var rule = CreateValidRecurringRule();

            rule.Id.Should().NotBe(Guid.Empty);
        }

        [Fact]
        public void RecurringRule_Without_Account_Should_Fail()
        {
            Action act = () =>
                new RecurringRule(
                    Guid.Empty,
                    Guid.NewGuid(),
                    new Money(100),
                    TransactionType.Expense,
                    PaymentMethod.Pix,
                    RecurrenceFrequency.Monthly,
                    DateTime.Today,
                    "Teste"
                );

            act.Should().Throw<DomainException>();
        }

        [Fact]
        public void RecurringRule_Without_Category_Should_Fail()
        {
            Action act = () =>
                new RecurringRule(
                    Guid.NewGuid(),
                    Guid.Empty,
                    new Money(100),
                    TransactionType.Expense,
                    PaymentMethod.Pix,
                    RecurrenceFrequency.Monthly,
                    DateTime.Today,
                    "Teste"
                );

            act.Should().Throw<DomainException>();
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("   ")]
        public void RecurringRule_Without_Description_Should_Fail(string description)
        {
            Action act = () =>
                new RecurringRule(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    new Money(100),
                    TransactionType.Expense,
                    PaymentMethod.Pix,
                    RecurrenceFrequency.Monthly,
                    DateTime.Today,
                    description
                );

            act.Should().Throw<DomainException>();
        }

        [Fact]
        public void EndDate_Before_Or_Equal_StartDate_Should_Fail()
        {
            var start = new DateTime(2025, 1, 10);
            var end = new DateTime(2025, 1, 10);

            Action act = () =>
                CreateValidRecurringRule(startDate: start, endDate: end);

            act.Should().Throw<DomainException>();
        }

        // =========================
        // PAYMENT METHOD RULES
        // =========================

        [Fact]
        public void CreditCard_RecurringRule_Must_Have_CreditCardId()
        {
            Action act = () =>
                new RecurringRule(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    new Money(100),
                    TransactionType.Expense,
                    PaymentMethod.CreditCard,
                    RecurrenceFrequency.Monthly,
                    DateTime.Today,
                    "Teste"
                );

            act.Should().Throw<DomainException>();
        }
        
        // =========================
        // GENERATE TRANSACTIONS
        // =========================

        [Fact]
        public void RecurringRule_Should_Generate_Monthly_Transactions()
        {
            var rule = CreateValidRecurringRule();
            var until = new DateTime(2025, 4, 1);

            var transactions = rule.GenerateTransactions(until).ToList();

            transactions.Should().HaveCount(4);
        }

        [Fact]
        public void RecurringRule_Should_Respect_Frequency()
        {
            var rule = CreateValidRecurringRule(
                frequency: RecurrenceFrequency.Quarterly,
                startDate: new DateTime(2025, 1, 1)
            );

            var transactions = rule.GenerateTransactions(new DateTime(2025, 7, 1)).ToList();

            transactions.Should().HaveCount(3);
            transactions[1].OccurredAt.Should().Be(new DateTime(2025, 4, 1));
        }

        [Fact]
        public void RecurringRule_Should_Not_Generate_Beyond_EndDate()
        {
            var rule = CreateValidRecurringRule(
                startDate: new DateTime(2025, 1, 1),
                endDate: new DateTime(2025, 3, 1)
            );

            var transactions = rule.GenerateTransactions(new DateTime(2025, 6, 1)).ToList();

            transactions.Should().HaveCount(3);
        }

        [Fact]
        public void Generated_Transactions_Should_Have_Correct_Values()
        {
            var rule = CreateValidRecurringRule();
            var transactions = rule.GenerateTransactions(new DateTime(2025, 3, 1)).ToList();

            transactions.Should().OnlyContain(t =>
                t.Amount == rule.Amount &&
                t.CategoryId == rule.CategoryId &&
                t.AccountId == rule.AccountId &&
                t.Status == TransactionStatus.Pending
            );
        }

        // =========================
        // CANCEL RULES
        // =========================

        [Fact]
        public void RecurringRule_Can_Be_Cancelled()
        {
            var rule = CreateValidRecurringRule();

            rule.Cancel();

            rule.IsCancelled.Should().BeTrue();
        }

        [Fact]
        public void Cancelled_RecurringRule_Cannot_Generate_Transactions()
        {
            var rule = CreateValidRecurringRule();
            rule.Cancel();

            Action act = () => rule.GenerateTransactions(DateTime.Today).ToList();

            act.Should().Throw<DomainException>();
        }

        // =========================
        // IMMUTABILITY
        // =========================

        [Fact]
        public void Generating_Transactions_Should_Not_Mutate_RecurringRule()
        {
            var rule = CreateValidRecurringRule();
            var id = rule.Id;
            var amount = rule.Amount;

            rule.GenerateTransactions(DateTime.Today.AddMonths(3)).ToList();

            rule.Id.Should().Be(id);
            rule.Amount.Should().Be(amount);
            rule.IsCancelled.Should().BeFalse();
        }
    }