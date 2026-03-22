public class Household
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }

    protected Household() { }

    public Household(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Nome do lar é obrigatório.");

        Id = Guid.NewGuid();
        Name = name.Trim();
        CreatedAt = DateTime.UtcNow;
    }

    public static Household Create(string name) => new(name);
}
