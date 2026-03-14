using PersonalBudget.Application.Interfaces;

public class TransactionService : ITransactionService
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly ITransactionQueryRepository _transactionQueryRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly ICreditCardStatementRepository _creditCardStatementRepository;
    private readonly ICreditCardRepository _creditCardRepository;
    private readonly IReadOnlyDictionary<PaymentMethod, ITransactionCreationStrategy> _creationStrategies;

    public TransactionService(
        ITransactionRepository transactionRepository,
        ITransactionQueryRepository transactionQueryRepository,
        IAccountRepository accountRepository,
        ICreditCardStatementRepository creditCardStatementRepository,
        ICreditCardRepository creditCardRepository,
        IEnumerable<ITransactionCreationStrategy> creationStrategies)
    {
        _transactionRepository = transactionRepository;
        _transactionQueryRepository = transactionQueryRepository;
        _accountRepository = accountRepository;
        _creditCardStatementRepository = creditCardStatementRepository;
        _creditCardRepository = creditCardRepository;
        _creationStrategies = creationStrategies.ToDictionary(s => s.PaymentMethod);
    }

    public Task<Guid> CreateAsync(CreateTransactionCommand command)
    {
        if (!_creationStrategies.TryGetValue(command.PaymentMethod, out var strategy))
            throw new DomainException($"Método de pagamento não suportado: {command.PaymentMethod}");

        // Aqui é onde o Strategy é aplicado:
        // PaymentMethod -> estratégia -> CreateAsync da estratégia.
        return strategy.CreateAsync(command);
    }

    public async Task<Transaction> GetByIdAsync(Guid transactionId)
    {
        var transaction = await _transactionRepository.GetByIdAsync(transactionId);
        if (transaction is null)
            throw new DomainException("Transação não encontrada.");
        return transaction;
    }

    public async Task<IEnumerable<GetAllTransactionByUserResponse>> GetByUserAndMonthAsync(GetAllTransactionByUserAndMonthQuery query)
    {
        return await _transactionQueryRepository.GetByUserAndMonthAsync(query.UserId, query.Month, query.Year);
    }

    public async Task<IEnumerable<GetAllTransactionByUserResponse>> GetByUserAsync(GetAllTransactionByUserQuery query)
    {
        return await _transactionQueryRepository.GetByUserAsync(query.UserId);
    }

    public async Task<IEnumerable<GetAllTransactionByUserResponse>> GetTransactionByCreditCardStatementAndMonthQuery(GetAllTransactionByCreditCardStatementAndMonthYearQuery query)
    {
        return await _transactionQueryRepository.GetAllTransactionByCreditCardStatementAndMonthYearQuery(query.userId, query.creditCardId, query.month, query.year);
    }

    public async Task UpdateStatusAsync(UpdateTransactionStatusCommand command)
    {
        var transaction = await _transactionRepository.GetByIdAsync(command.TransactionId);

        if (transaction is null || transaction.UserId != command.UserId)
            throw new DomainException("Transação não encontrada.");

        var previousStatus = transaction.Status;

        if (command.Status == TransactionStatus.Pending && previousStatus == TransactionStatus.Completed)
        {
            var account = await _accountRepository.GetByIdAsync(transaction.AccountId);
            if (account is null)
                throw new DomainException("Conta não encontrada.");
            TransactionApplier.Revert(account, transaction);
            transaction.SetStatus(command.Status);
            await _accountRepository.UpdateAsync(account);
        }
        else
        {
            transaction.SetStatus(command.Status);

            if (command.Status == TransactionStatus.Completed && previousStatus != TransactionStatus.Completed)
            {
                var account = await _accountRepository.GetByIdAsync(transaction.AccountId);
                if (account is null)
                    throw new DomainException("Conta não encontrada.");
                TransactionApplier.Apply(account, transaction);
                await _accountRepository.UpdateAsync(account);
            }
        }

        await _transactionRepository.UpdateAsync(transaction);
    }

    public async Task UpdateStatusToCreditCardStatementAsync(Guid userId, UpdateTransactionStatusToCreditCardStatementCommand command)
    {
        var statement = await _creditCardStatementRepository.GetByCreditCardAndClosingMonthYearAsync(
            command.CreditCardId, command.Month, command.Year);

        if (statement is null)
            throw new DomainException("Fatura do cartão de crédito não encontrada para o período informado.");

        //if (statement.Status != BillStatus.Closed)
        //    throw new DomainException("A fatura deve estar fechada antes de ser paga.");

        var creditCard = await _creditCardRepository.GetByIdAsync(command.CreditCardId);
        if (creditCard is null || creditCard.UserId != userId)
            throw new DomainException("Cartão de crédito não encontrado.");

        var account = await _accountRepository.GetByIdAsync(creditCard.AccountId);
        if (account is null)
            throw new DomainException("Conta associada ao cartão de crédito não encontrada.");

        if (account.Balance.Amount < statement.TotalAmount.Amount)
            throw new DomainException("Saldo insuficiente na conta para pagar a fatura do cartão de crédito.");

        // Se a fatura já estiver paga, tratamos a operação como idempotente:
        // não debitamos novamente a conta, apenas garantimos que todas as
        // transações vinculadas à fatura estejam concluídas.
        if (statement.Status == BillStatus.Paid)
        {
            var alreadyPaidTransactions = await _transactionRepository.GetByStatementIdAsync(statement.Id);
            foreach (var transaction in alreadyPaidTransactions.Where(x => x.Status == TransactionStatus.Pending))
            {
                transaction.Complete();
                await _transactionRepository.UpdateAsync(transaction);
            }

            return;
        }

        account.Debit(statement.TotalAmount);
        await _accountRepository.UpdateAsync(account);

        statement.MarkAsPaid();
        await _creditCardStatementRepository.UpdateAsync(statement);

        var transactions = await _transactionRepository.GetByStatementIdAsync(statement.Id);
        foreach (var transaction in transactions)
        {
            if (transaction.Status == TransactionStatus.Pending)
            {
                transaction.Complete();
                await _transactionRepository.UpdateAsync(transaction);
            }
        }
    }
}