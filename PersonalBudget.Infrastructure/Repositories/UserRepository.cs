
using Microsoft.EntityFrameworkCore;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(User user)
    {
        _context.Users.Add(user);
        await SaveChangesAsync();
    }

    public async Task<User?> GetByEmailAsync(Email email)
    {
        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email.Value == email.Value);
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<IReadOnlyList<User>> GetByIdsAsync(IReadOnlyList<Guid> ids)
    {
        if (ids.Count == 0)
            return [];

        return await _context.Users
            .AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .ToListAsync();
    }

    /// <summary>
    /// Reads are AsNoTracking, so mutating a User and calling SaveChangesAsync
    /// persists nothing - the change tracker never saw the entity. Anything
    /// that modifies a User has to come through here.
    /// </summary>
    public async Task UpdateAsync(User user)
    {
        _context.Users.Update(user);
        await SaveChangesAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
