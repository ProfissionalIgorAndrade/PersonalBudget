
using Microsoft.EntityFrameworkCore;

public class TransactionQueryRepository : ITransactionQueryRepository
{
    private readonly AppDbContext _context;

    public TransactionQueryRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<GetAllTransactionByUserResponse>> GetByUserAndMonthAsync(Guid userId, int month, int year)
    {
        return await _context.Transactions
            .Where(t => t.UserId == userId && t.Date.Value.Month == month && t.Date.Value.Year == year)
            .Join(_context.Accounts,
                t => t.AccountId,
                a => a.Id,
                (t, a) => new { Transaction = t, Account = a })
            .GroupJoin(_context.Categories,
                ta => ta.Transaction.CategoryId,
                c => c.Id,
                (ta, c) => new { ta.Transaction, ta.Account, Category = c.FirstOrDefault() })
            .GroupJoin(_context.CreditCards,
                tac => tac.Transaction.CreditCardId,
                cc => cc.Id,
                (tac, cc) => new { tac.Transaction, tac.Account, tac.Category, CreditCard = cc.FirstOrDefault() })
            .OrderByDescending(tacc => tacc.Transaction.Date)
            .Select(tacc => new GetAllTransactionByUserResponse(
                tacc.Transaction.Id,
                    tacc.Transaction.AccountId,
                    tacc.Account.Agency.Value,
                    tacc.Transaction.CategoryId,
                    tacc.Category != null ? tacc.Category.Name : null,
                    tacc.Category != null ? tacc.Category.Type.ToString() : null,
                    tacc.Transaction.CreditCardId,
                    tacc.CreditCard != null ? tacc.CreditCard.Name : null,
                    tacc.Transaction.Type.ToString(),
                    tacc.Transaction.Status.ToString(),
                    tacc.Transaction.PaymentMethod.ToString(),
                    tacc.Transaction.Amount.Amount,
                    tacc.Transaction.Date.Value,
                    tacc.Transaction.Description.Value
             ))
            .AsNoTracking()
            .ToListAsync();
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
                    t.Type.ToString(),
                    t.Status.ToString(),
                    t.PaymentMethod.ToString(),
                    t.Amount.Amount,
                    t.Date.Value,
                    t.Description.Value
             ))
            .AsNoTracking()
            .ToListAsync();
    }
}