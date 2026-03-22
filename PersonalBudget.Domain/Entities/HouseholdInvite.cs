public class HouseholdInvite
{
    public Guid Id { get; private set; }
    public Guid HouseholdId { get; private set; }
    public Guid InviterUserId { get; private set; }
    public string InviteeEmailNormalized { get; private set; } = null!;
    public string Token { get; private set; } = null!;
    public HouseholdInviteStatus Status { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    protected HouseholdInvite() { }

    public HouseholdInvite(
        Guid householdId,
        Guid inviterUserId,
        string inviteeEmailNormalized,
        string token,
        TimeSpan validity)
    {
        if (householdId == Guid.Empty || inviterUserId == Guid.Empty)
            throw new DomainException("Lar e quem convida são obrigatórios.");
        if (string.IsNullOrWhiteSpace(inviteeEmailNormalized))
            throw new DomainException("E-mail do convidado é obrigatório.");
        if (string.IsNullOrWhiteSpace(token))
            throw new DomainException("Token do convite é obrigatório.");

        Id = Guid.NewGuid();
        HouseholdId = householdId;
        InviterUserId = inviterUserId;
        InviteeEmailNormalized = inviteeEmailNormalized.Trim().ToLowerInvariant();
        Token = token;
        Status = HouseholdInviteStatus.Pending;
        CreatedAt = DateTime.UtcNow;
        ExpiresAt = CreatedAt.Add(validity);
    }

    public void Accept()
    {
        if (Status != HouseholdInviteStatus.Pending)
            throw new DomainException("Convite não está pendente.");
        if (DateTime.UtcNow > ExpiresAt)
        {
            Status = HouseholdInviteStatus.Expired;
            throw new DomainException("Convite expirado.");
        }
        Status = HouseholdInviteStatus.Accepted;
    }

    public void MarkExpiredIfNeeded()
    {
        if (Status == HouseholdInviteStatus.Pending && DateTime.UtcNow > ExpiresAt)
            Status = HouseholdInviteStatus.Expired;
    }

    public void Revoke()
    {
        if (Status == HouseholdInviteStatus.Accepted)
            throw new DomainException("Convite já aceito não pode ser revogado.");
        Status = HouseholdInviteStatus.Revoked;
    }
}
