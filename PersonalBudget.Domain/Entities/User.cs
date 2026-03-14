public class User
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public Email Email { get; set; }
    public PasswordHash PasswordHash { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    public User(string name, Email email, PasswordHash passwordHash)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Nome do usuário é obrigatório.");

        Name = name;
        Email = email ?? throw new DomainException("E-mail é obrigatório.");
        PasswordHash = passwordHash ?? throw new DomainException("Hash da senha é obrigatório.");

        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        IsActive = true;
    }
    
    protected User() { }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void ChangePassword(PasswordHash newPasswordHash)
    {
        if (!IsActive)
            throw new DomainException("Usuário inativo não pode alterar a senha.");

        PasswordHash = newPasswordHash
            ?? throw new DomainException("Hash da senha é obrigatório.");
    }

    public bool CanAuthenticate(PasswordHash providedPasswordHash)
    {
        if (!IsActive)
            return false;

        return PasswordHash.Equals(providedPasswordHash);
    }
}