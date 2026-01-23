public class User
{
    public User(string name, Email email, PasswordHash passwordHash)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("User name is required.");

        Name = name;
        Email = email ?? throw new DomainException("Email is required.");
        PasswordHash = passwordHash ?? throw new DomainException("Password hash is required.");

        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        IsActive = true;
    }

    public Guid Id { get; set; }
    public string Name { get; set; }
    public Email Email { get; set; }
    public PasswordHash PasswordHash { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void ChangePassword(PasswordHash newPasswordHash)
    {
        if (!IsActive)
            throw new DomainException("Inactive user cannot change password.");

        PasswordHash = newPasswordHash
            ?? throw new DomainException("Password hash is required.");
    }

    public bool CanAuthenticate(PasswordHash providedPasswordHash)
    {
        if (!IsActive)
            return false;

        return PasswordHash.Equals(providedPasswordHash);
    }
}