using Microsoft.EntityFrameworkCore;
using PersonalBudget.Application.DTOs.CreditCard;

public class TransactionQueryRepository : ITransactionQueryRepository
{
    private readonly AppDbContext _context;

    public TransactionQueryRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<GetAllTransactionByUserResponse>> GetAllTransactionByCreditCardStatementAndMonthYearQuery(Guid userId, Guid creditCardId, int month, int year)
    {
        return await
            (from t in _context.Transactions
             join s in _context.CreditCardStatements
                 on t.CreditCardId equals s.CreditCardId
             join a in _context.Accounts
                 on t.AccountId equals a.Id
             from c in _context.Categories
                 .Where(c => t.CategoryId != null && c.Id == t.CategoryId)
                 .DefaultIfEmpty()
             from cc in _context.CreditCards
                 .Where(cc => cc.Id == t.CreditCardId)
                 .DefaultIfEmpty()
             where t.UserId == userId
                 && t.CreditCardId == creditCardId
                 && t.Date.Value.Month == month
                 && t.Date.Value.Year == year
                 && s.Status == BillStatus.Open
                 && t.Date.Value >= s.PeriodStart
                 && t.Date.Value <= s.PeriodEnd
             orderby t.Date.Value descending
             select new GetAllTransactionByUserResponse(
                 t.Id,
                 t.AccountId,
                 a.Agency.Value,
                 t.CategoryId,
                 c != null ? c.Name : null,
                 c != null ? c.Type.ToString() : null,
                 t.CreditCardId,
                 cc != null ? cc.Name : null,
                 t.TransferId,
                 t.Type.ToString(),
                 t.Status.ToString(),
                 t.PaymentMethod.ToString(),
                 t.Frequency.ToString(),
                 t.ExpirationDate,
                 t.Amount.Amount,
                 t.Date.Value,
                 t.Description.Value
             ))
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IReadOnlyList<StatementTransactionItemDto>> GetTransactionDetailsByStatementIdAsync(Guid statementId)
    {
        var query =
            from t in _context.Transactions
            from c in _context.Categories
                .Where(c => t.CategoryId != null && c.Id == t.CategoryId)
                .DefaultIfEmpty()
            where t.StatementId == statementId
            orderby t.Date.Value descending
            select new StatementTransactionItemDto(
                t.Id,
                t.Date.Value,
                t.Description.Value,
                t.Amount.Amount,
                c != null ? c.Name : null,
                t.Status.ToString()
            );

        return await query.AsNoTracking().ToListAsync();
    }

    public async Task<(IReadOnlyList<StatementTransactionItemDto> Items, int TotalCount)> GetTransactionDetailsByStatementIdPagedAsync(
        Guid statementId, int page, int pageSize)
    {
        var query =
            from t in _context.Transactions
            from c in _context.Categories
                .Where(c => t.CategoryId != null && c.Id == t.CategoryId)
                .DefaultIfEmpty()
            where t.StatementId == statementId
            orderby t.Date.Value descending
            select new StatementTransactionItemDto(
                t.Id,
                t.Date.Value,
                t.Description.Value,
                t.Amount.Amount,
                c != null ? c.Name : null,
                t.Status.ToString()
            );

        var totalCount = await query.AsNoTracking().CountAsync();
        var items = await query
            .AsNoTracking()
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<GetAllTransactionByUserResponse>> GetByUserAndMonthAsync(Guid userId, int month, int year)
    {
        var query =
            from t in _context.Transactions
            join a in _context.Accounts
                on t.AccountId equals a.Id
            from c in _context.Categories
                .Where(c => t.CategoryId != null && c.Id == t.CategoryId)
                .DefaultIfEmpty()
            from cc in _context.CreditCards
                .Where(cc => t.CreditCardId != null && cc.Id == t.CreditCardId)
                .DefaultIfEmpty()
            where t.UserId == userId && t.Date.Value.Month == month && t.Date.Value.Year == year
            orderby t.Date.Value descending
            select new GetAllTransactionByUserResponse(
                t.Id,
                t.AccountId,
                a.Agency.Value,
                t.CategoryId,
                c != null ? c.Name : null,
                c != null ? c.Type.ToString() : null,
                t.CreditCardId,
                cc != null ? cc.Name : null,
                t.TransferId,
                t.Type.ToString(),
                t.Status.ToString(),
                t.PaymentMethod.ToString(),
                t.Frequency.ToString(),
                t.ExpirationDate,
                t.Amount.Amount,
                t.Date.Value,
                t.Description.Value
            );

        return await query.AsNoTracking().ToListAsync();
    }

    public async Task<(IReadOnlyList<GetAllTransactionByUserResponse> Items, int TotalCount)> GetByUserAndMonthPagedAsync(
        Guid userId, int month, int year, int page, int pageSize)
    {
        var query =
            from t in _context.Transactions
            join a in _context.Accounts
                on t.AccountId equals a.Id
            from c in _context.Categories
                .Where(c => t.CategoryId != null && c.Id == t.CategoryId)
                .DefaultIfEmpty()
            from cc in _context.CreditCards
                .Where(cc => t.CreditCardId != null && cc.Id == t.CreditCardId)
                .DefaultIfEmpty()
            where t.UserId == userId && t.Date.Value.Month == month && t.Date.Value.Year == year
            orderby t.Date.Value descending
            select new GetAllTransactionByUserResponse(
                t.Id,
                t.AccountId,
                a.Agency.Value,
                t.CategoryId,
                c != null ? c.Name : null,
                c != null ? c.Type.ToString() : null,
                t.CreditCardId,
                cc != null ? cc.Name : null,
                t.TransferId,
                t.Type.ToString(),
                t.Status.ToString(),
                t.PaymentMethod.ToString(),
                t.Frequency.ToString(),
                t.ExpirationDate,
                t.Amount.Amount,
                t.Date.Value,
                t.Description.Value
            );

        var totalCount = await query.AsNoTracking().CountAsync();
        var items = await query
            .AsNoTracking()
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<GetAllTransactionByUserResponse>> GetByAccountAndMonthAsync(Guid userId, Guid accountId, int month, int year)
    {
        var query =
            from t in _context.Transactions
            join a in _context.Accounts
                on t.AccountId equals a.Id
            from c in _context.Categories
                .Where(c => t.CategoryId != null && c.Id == t.CategoryId)
                .DefaultIfEmpty()
            from cc in _context.CreditCards
                .Where(cc => t.CreditCardId != null && cc.Id == t.CreditCardId)
                .DefaultIfEmpty()
            where t.UserId == userId
               && t.AccountId == accountId
               && t.Date.Value.Month == month
               && t.Date.Value.Year == year
               && t.PaymentMethod != PaymentMethod.CreditCard
            orderby t.Date.Value descending
            select new GetAllTransactionByUserResponse(
                t.Id,
                t.AccountId,
                a.Agency.Value,
                t.CategoryId,
                c != null ? c.Name : null,
                c != null ? c.Type.ToString() : null,
                t.CreditCardId,
                cc != null ? cc.Name : null,
                t.TransferId,
                t.Type.ToString(),
                t.Status.ToString(),
                t.PaymentMethod.ToString(),
                t.Frequency.ToString(),
                t.ExpirationDate,
                t.Amount.Amount,
                t.Date.Value,
                t.Description.Value
            );

        return await query.AsNoTracking().ToListAsync();
    }

    public async Task<(IReadOnlyList<GetAllTransactionByUserResponse> Items, int TotalCount)> GetByAccountAndMonthPagedAsync(
        Guid userId, Guid accountId, int month, int year, int page, int pageSize)
    {
        var query =
            from t in _context.Transactions
            join a in _context.Accounts
                on t.AccountId equals a.Id
            from c in _context.Categories
                .Where(c => t.CategoryId != null && c.Id == t.CategoryId)
                .DefaultIfEmpty()
            from cc in _context.CreditCards
                .Where(cc => t.CreditCardId != null && cc.Id == t.CreditCardId)
                .DefaultIfEmpty()
            where t.UserId == userId
               && t.AccountId == accountId
               && t.Date.Value.Month == month
               && t.Date.Value.Year == year
               && t.PaymentMethod != PaymentMethod.CreditCard
            orderby t.Date.Value descending
            select new GetAllTransactionByUserResponse(
                t.Id,
                t.AccountId,
                a.Agency.Value,
                t.CategoryId,
                c != null ? c.Name : null,
                c != null ? c.Type.ToString() : null,
                t.CreditCardId,
                cc != null ? cc.Name : null,
                t.TransferId,
                t.Type.ToString(),
                t.Status.ToString(),
                t.PaymentMethod.ToString(),
                t.Frequency.ToString(),
                t.ExpirationDate,
                t.Amount.Amount,
                t.Date.Value,
                t.Description.Value
            );

        var totalCount = await query.AsNoTracking().CountAsync();
        var items = await query
            .AsNoTracking()
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<GetAllTransactionByUserResponse>> GetByUserAsync(Guid userId)
    {
        return await
            (from t in _context.Transactions
             join a in _context.Accounts
                 on t.AccountId equals a.Id
             from c in _context.Categories
                 .Where(c => t.CategoryId != null && c.Id == t.CategoryId)
                 .DefaultIfEmpty()
             from cc in _context.CreditCards
                 .Where(cc => t.CreditCardId != null && cc.Id == t.CreditCardId)
                 .DefaultIfEmpty()
             where t.UserId == userId
             orderby t.Date.Value descending
             select new GetAllTransactionByUserResponse(
                 t.Id,
                    t.AccountId,
                    a.Agency.Value,
                    t.CategoryId,
                    c != null ? c.Name : null,
                    c != null ? c.Type.ToString() : null,
                    t.CreditCardId,
                    cc != null ? cc.Name : null,
                    t.TransferId,
                    t.Type.ToString(),
                    t.Status.ToString(),
                    t.PaymentMethod.ToString(),
                    t.Frequency.ToString(),
                    t.ExpirationDate,
                    t.Amount.Amount,
                    t.Date.Value,
                    t.Description.Value
             ))
            .AsNoTracking()
            .ToListAsync();
    }
}