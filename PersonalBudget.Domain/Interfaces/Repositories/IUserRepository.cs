public interface IUserRepository
{
    Task<User?> GetByEmailAsync(Email email);
    Task<User?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<User>> GetByIdsAsync(IReadOnlyList<Guid> ids);
    Task AddAsync(User user);
    Task UpdateAsync(User user);
    Task SaveChangesAsync();
}