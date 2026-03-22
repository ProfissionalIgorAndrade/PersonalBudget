using System.Security.Cryptography;
using PersonalBudget.Application.DTOs.Household;
using PersonalBudget.Application.Interfaces;

public class HouseholdService : IHouseholdService
{
    private readonly IHouseholdRepository _households;
    private readonly IHouseholdMembershipRepository _memberships;
    private readonly IHouseholdMemberProfileRepository _profiles;
    private readonly IHouseholdInviteRepository _invites;
    private readonly IUserRepository _users;
    private readonly ITransactionQueryRepository _transactionQueries;

    public HouseholdService(
        IHouseholdRepository households,
        IHouseholdMembershipRepository memberships,
        IHouseholdMemberProfileRepository profiles,
        IHouseholdInviteRepository invites,
        IUserRepository users,
        ITransactionQueryRepository transactionQueries)
    {
        _households = households;
        _memberships = memberships;
        _profiles = profiles;
        _invites = invites;
        _users = users;
        _transactionQueries = transactionQueries;
    }

    public async Task<IReadOnlyList<HouseholdListItemDto>> ListForUserAsync(Guid userId)
    {
        var ids = await _memberships.GetHouseholdIdsByUserAsync(userId);
        var list = await _households.GetByIdsAsync(ids);
        return list.Select(h => new HouseholdListItemDto(h.Id, h.Name)).ToList();
    }

    public async Task<IReadOnlyList<HouseholdMemberProfileResponseDto>> GetProfilesAsync(Guid userId, Guid householdId)
    {
        if (!await _memberships.IsMemberAsync(userId, householdId))
            throw new DomainException("Lar não encontrado ou sem permissão.");

        var profiles = await _profiles.GetByHouseholdAsync(householdId);
        return profiles.Select(p => new HouseholdMemberProfileResponseDto(
            p.Id,
            p.DisplayName,
            p.Kind.ToString(),
            p.UserId)).ToList();
    }

    public async Task<HouseholdMemberProfileResponseDto> CreateJointProfileAsync(Guid userId, Guid householdId, string displayName)
    {
        if (!await _memberships.IsMemberAsync(userId, householdId))
            throw new DomainException("Lar não encontrado ou sem permissão.");

        if (string.IsNullOrWhiteSpace(displayName))
            throw new DomainException("Nome do correspondente é obrigatório.");

        var existing = await _profiles.GetByHouseholdAsync(householdId);
        var sortOrder = existing.Count == 0 ? 0 : existing.Max(p => p.SortOrder) + 1;

        var profile = HouseholdMemberProfile.CreateJoint(displayName.Trim(), householdId, sortOrder);
        await _profiles.AddAsync(profile);

        return new HouseholdMemberProfileResponseDto(
            profile.Id,
            profile.DisplayName,
            profile.Kind.ToString(),
            profile.UserId);
    }

    public async Task<string> CreateInviteAsync(Guid inviterUserId, Guid householdId, string inviteeEmail)
    {
        if (!await _memberships.IsMemberAsync(inviterUserId, householdId))
            throw new DomainException("Sem permissão para convidar neste lar.");

        var email = new Email(inviteeEmail);
        var invitee = await _users.GetByEmailAsync(email);
        if (invitee is null)
            throw new DomainException("E-mail não cadastrado no sistema.");
        if (invitee.Id == inviterUserId)
            throw new DomainException("Não é possível convidar a si mesmo.");

        if (await _memberships.IsMemberAsync(invitee.Id, householdId))
            throw new DomainException("Usuário já pertence a este lar.");

        var normalized = email.Value.Trim().ToLowerInvariant();
        var pending = await _invites.GetPendingByHouseholdAndEmailAsync(householdId, normalized);
        if (pending.Count > 0)
            throw new DomainException("Já existe convite pendente para este e-mail.");

        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var invite = new HouseholdInvite(householdId, inviterUserId, normalized, token, TimeSpan.FromDays(7));
        await _invites.AddAsync(invite);
        return token;
    }

    public async Task AcceptInviteAsync(Guid userId, string token)
    {
        var invite = await _invites.GetByTokenAsync(token);
        if (invite is null)
            throw new DomainException("Convite inválido.");

        invite.MarkExpiredIfNeeded();
        if (invite.Status == HouseholdInviteStatus.Expired)
        {
            await _invites.SaveChangesAsync();
            throw new DomainException("Convite expirado.");
        }

        if (invite.Status != HouseholdInviteStatus.Pending)
            throw new DomainException("Convite não está mais pendente.");

        var user = await _users.GetByIdAsync(userId);
        if (user is null)
            throw new DomainException("Usuário não encontrado.");

        if (!string.Equals(user.Email.Value.Trim(), invite.InviteeEmailNormalized, StringComparison.OrdinalIgnoreCase))
            throw new DomainException("Este convite foi enviado para outro e-mail.");

        invite.Accept();

        await _memberships.AddAsync(HouseholdMembership.CreateMember(invite.HouseholdId, userId));

        var existing = await _profiles.GetByHouseholdAsync(invite.HouseholdId);
        var sortOrder = existing.Count;
        var displayName = string.IsNullOrWhiteSpace(user.Name) ? "Membro" : user.Name.Trim();
        await _profiles.AddAsync(HouseholdMemberProfile.CreateLinkedUser(invite.HouseholdId, userId, displayName, sortOrder));
    }

    public async Task<IReadOnlyList<HouseholdProfileSummaryRow>> GetSummaryByProfileAsync(
        Guid userId, Guid householdId, int month, int year)
    {
        if (!await _memberships.IsMemberAsync(userId, householdId))
            throw new DomainException("Lar não encontrado ou sem permissão.");

        return await _transactionQueries.GetHouseholdSummaryByProfileAsync(householdId, month, year);
    }
}
