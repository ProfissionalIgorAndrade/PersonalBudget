public interface IUserRepository
{
    Task<User?> GetByEmailAsync(Email email);
    Task AddAsync(User user);
    Task SaveChangesAsync();
}