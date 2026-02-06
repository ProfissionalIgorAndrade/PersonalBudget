
using Microsoft.EntityFrameworkCore;

public class TransactionQueryRepository : ITransactionQueryRepository
{
    private readonly AppDbContext _context;

    public TransactionQueryRepository(AppDbContext context)
    {
        _context = context;
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