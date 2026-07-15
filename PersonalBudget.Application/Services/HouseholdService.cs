using System.Linq;
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
    private readonly IAccountRepository _accounts;
    private readonly ICategoryRepository _categories;
    private readonly ICreditCardRepository _creditCards;
    private readonly ITransactionRepository _transactions;

    public HouseholdService(
        IHouseholdRepository households,
        IHouseholdMembershipRepository memberships,
        IHouseholdMemberProfileRepository profiles,
        IHouseholdInviteRepository invites,
        IUserRepository users,
        ITransactionQueryRepository transactionQueries,
        IAccountRepository accounts,
        ICategoryRepository categories,
        ICreditCardRepository creditCards,
        ITransactionRepository transactions)
    {
        _households = households;
        _memberships = memberships;
        _profiles = profiles;
        _invites = invites;
        _users = users;
        _transactionQueries = transactionQueries;
        _accounts = accounts;
        _categories = categories;
        _creditCards = creditCards;
        _transactions = transactions;
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
            p.UserId,
            p.Emoji,
            p.Color)).ToList();
    }

    public async Task<HouseholdMemberProfileResponseDto> CreateJointProfileAsync(Guid userId, Guid householdId, string displayName, string? emoji = null, string? color = null)
    {
        if (!await _memberships.IsMemberAsync(userId, householdId))
            throw new DomainException("Lar não encontrado ou sem permissão.");

        if (string.IsNullOrWhiteSpace(displayName))
            throw new DomainException("Nome do correspondente é obrigatório.");

        var existing = await _profiles.GetByHouseholdAsync(householdId);
        var sortOrder = existing.Count == 0 ? 0 : existing.Max(p => p.SortOrder) + 1;

        var profile = HouseholdMemberProfile.CreateJoint(displayName.Trim(), householdId, sortOrder, color, emoji);
        await _profiles.AddAsync(profile);

        return new HouseholdMemberProfileResponseDto(
            profile.Id,
            profile.DisplayName,
            profile.Kind.ToString(),
            profile.UserId,
            profile.Emoji,
            profile.Color);
    }

    public async Task<HouseholdMemberProfileResponseDto> UpdateProfileAsync(Guid userId, Guid householdId, Guid profileId, string displayName, string? emoji, string? color)
    {
        if (!await _memberships.IsMemberAsync(userId, householdId))
            throw new DomainException("Lar não encontrado ou sem permissão.");

        var profile = await _profiles.GetByIdAsync(profileId);
        if (profile is null || profile.HouseholdId != householdId)
            throw new DomainException("Perfil não encontrado.");

        profile.Update(displayName, color, emoji);
        await _profiles.SaveChangesAsync();

        return new HouseholdMemberProfileResponseDto(
            profile.Id,
            profile.DisplayName,
            profile.Kind.ToString(),
            profile.UserId,
            profile.Emoji,
            profile.Color);
    }

    public async Task DeleteProfileAndMergeAsync(
        Guid userId,
        Guid householdId,
        Guid removeProfileId,
        Guid mergeIntoProfileId)
    {
        if (!await _memberships.IsMemberAsync(userId, householdId))
            throw new DomainException("Lar não encontrado ou sem permissão.");

        if (removeProfileId == mergeIntoProfileId)
            throw new DomainException("O perfil de destino deve ser diferente do perfil removido.");

        var removeProfile = await _profiles.GetByIdAsync(removeProfileId);
        var mergeProfile = await _profiles.GetByIdAsync(mergeIntoProfileId);
        if (removeProfile is null || mergeProfile is null)
            throw new DomainException("Perfil não encontrado.");

        if (removeProfile.HouseholdId != householdId || mergeProfile.HouseholdId != householdId)
            throw new DomainException("Os perfis devem pertencer a este lar.");

        if (removeProfile.Kind == HouseholdMemberProfileKind.LinkedUser)
        {
            if (removeProfile.UserId != userId)
                throw new DomainException("Você só pode remover o seu próprio correspondente vinculado.");
        }

        var txs = (await _transactions.GetByHouseholdAndAttributionProfileAsync(householdId, removeProfileId))
            .ToList();
        foreach (var t in txs)
            t.ReassignAttributionProfileForMerge(mergeIntoProfileId);
        if (txs.Count > 0)
            await _transactions.BulkUpdateAsync(txs);

        if (removeProfile.Kind == HouseholdMemberProfileKind.LinkedUser &&
            mergeProfile.Kind == HouseholdMemberProfileKind.LinkedUser &&
            removeProfile.UserId is { } uRemove &&
            mergeProfile.UserId is { } uMerge &&
            uRemove != uMerge)
        {
            var accounts = (await _accounts.GetAllByHouseholdAndUserAsync(householdId, uRemove)).ToList();
            foreach (var a in accounts)
                a.ReassignUserId(uMerge);
            if (accounts.Count > 0)
                await _accounts.BulkUpdateAsync(accounts);

            var cards = (await _creditCards.GetAllByHouseholdAndUserAsync(householdId, uRemove)).ToList();
            foreach (var c in cards)
                c.ReassignUserId(uMerge);
            if (cards.Count > 0)
                await _creditCards.BulkUpdateAsync(cards);
        }

        await _profiles.RemoveAsync(removeProfile);
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
        await _invites.SaveChangesAsync();

        var targetHouseholdId = invite.HouseholdId;

        if (!await _memberships.IsMemberAsync(userId, targetHouseholdId))
            await _memberships.AddAsync(HouseholdMembership.CreateMember(targetHouseholdId, userId));

        var targetLinked = await _profiles.GetLinkedProfileForUserAsync(targetHouseholdId, userId);
        if (targetLinked is null)
        {
            var existingOnTarget = await _profiles.GetByHouseholdAsync(targetHouseholdId);
            var sortOrder = existingOnTarget.Count;
            var displayName = string.IsNullOrWhiteSpace(user.Name) ? "Membro" : user.Name.Trim();
            targetLinked = HouseholdMemberProfile.CreateLinkedUser(
                targetHouseholdId, userId, displayName, sortOrder);
            await _profiles.AddAsync(targetLinked);
        }

        var targetProfileId = targetLinked.Id;
        var myHouseholdIds = await _memberships.GetHouseholdIdsByUserAsync(userId);
        var sourceHouseholdIds = myHouseholdIds.Where(id => id != targetHouseholdId).ToList();

        foreach (var sourceId in sourceHouseholdIds)
        {
            await MigrateSourceHouseholdIntoTargetAsync(
                userId, sourceId, targetHouseholdId, targetProfileId);
        }

        await _memberships.RemoveAllExceptAsync(userId, targetHouseholdId);
    }

    private async Task MigrateSourceHouseholdIntoTargetAsync(
        Guid userId,
        Guid sourceHouseholdId,
        Guid targetHouseholdId,
        Guid targetLinkedProfileId)
    {
        var members = await _memberships.GetMembersByHouseholdAsync(sourceHouseholdId);
        if (members.Count != 1 || members[0].UserId != userId)
        {
            throw new DomainException(
                "Para fundir o seu lar ao do convite, você precisa ser o único membro do lar de origem. " +
                "Remova outros membros antes de aceitar, se for o caso.");
        }

        var oldLinked = await _profiles.GetLinkedProfileForUserAsync(sourceHouseholdId, userId);
        var oldLinkedId = oldLinked?.Id;

        var categoryMap = await BuildCategoryMapAsync(sourceHouseholdId, targetHouseholdId);

        var targetProfiles = (await _profiles.GetByHouseholdAsync(targetHouseholdId)).ToList();
        var nextOrder = targetProfiles.Count == 0
            ? 0
            : targetProfiles.Max(p => p.SortOrder) + 1;

        var sourceProfiles = (await _profiles.GetByHouseholdAsync(sourceHouseholdId)).ToList();
        var joints = sourceProfiles
            .Where(p => p.Kind == HouseholdMemberProfileKind.Joint)
            .OrderBy(p => p.SortOrder)
            .ToList();
        foreach (var jp in joints)
        {
            jp.RelocateToHousehold(targetHouseholdId, nextOrder++);
        }

        if (joints.Count > 0)
            await _profiles.BulkUpdateAsync(joints);

        var accounts = (await _accounts.GetAllByHouseholdIdAsync(sourceHouseholdId)).ToList();
        foreach (var a in accounts)
            a.RelocateToHousehold(targetHouseholdId);
        if (accounts.Count > 0)
            await _accounts.BulkUpdateAsync(accounts);

        var cards = (await _creditCards.GetAllByHouseholdAsync(sourceHouseholdId)).ToList();
        foreach (var c in cards)
            c.RelocateToHousehold(targetHouseholdId);
        if (cards.Count > 0)
            await _creditCards.BulkUpdateAsync(cards);

        var txs = (await _transactions.GetByHouseholdAsync(sourceHouseholdId)).ToList();
        foreach (var t in txs)
        {
            var newAttr = oldLinkedId.HasValue && t.AttributionProfileId == oldLinkedId.Value
                ? targetLinkedProfileId
                : t.AttributionProfileId;

            Guid? newCat = null;
            if (t.CategoryId.HasValue)
            {
                if (!categoryMap.TryGetValue(t.CategoryId.Value, out var mapped))
                    throw new DomainException("Falha ao mapear categoria na migração do convite.");

                newCat = mapped;
            }

            t.ApplyInviteAcceptanceMigration(targetHouseholdId, newAttr, newCat);
        }

        if (txs.Count > 0)
            await _transactions.BulkUpdateAsync(txs);

        if (oldLinked is not null)
            await _profiles.RemoveAsync(oldLinked);

        await _memberships.RemoveAsync(userId, sourceHouseholdId);
        await _categories.RemoveByHouseholdAsync(sourceHouseholdId);

        var remaining = await _memberships.GetMembersByHouseholdAsync(sourceHouseholdId);
        if (remaining.Count > 0)
            return;

        var orphanHousehold = await _households.GetByIdAsync(sourceHouseholdId);
        if (orphanHousehold is not null)
            await _households.RemoveAsync(orphanHousehold);
    }

    private async Task<Dictionary<Guid, Guid>> BuildCategoryMapAsync(
        Guid sourceHouseholdId,
        Guid targetHouseholdId)
    {
        var sourceCats = (await _categories.GetAllByHouseholdAsync(sourceHouseholdId)).ToList();
        var targetList = (await _categories.GetAllByHouseholdAsync(targetHouseholdId)).ToList();
        var map = new Dictionary<Guid, Guid>();

        foreach (var sc in sourceCats)
        {
            var match = targetList.FirstOrDefault(tc =>
                string.Equals(tc.Name, sc.Name, StringComparison.OrdinalIgnoreCase) &&
                tc.Type == sc.Type);
            if (match is not null)
            {
                map[sc.Id] = match.Id;
                continue;
            }

            Category created = sc.IsSystem
                ? new Category(targetHouseholdId, sc.Name, true, sc.Type, sc.Color, sc.Icon)
                : Category.Create(targetHouseholdId, sc.Name, sc.Type, sc.Color, sc.Icon);

            await _categories.AddAsync(created);
            targetList.Add(created);
            map[sc.Id] = created.Id;
        }

        return map;
    }

    public async Task<IReadOnlyList<HouseholdProfileSummaryRow>> GetSummaryByProfileAsync(
        Guid userId, Guid householdId, int month, int year)
    {
        if (!await _memberships.IsMemberAsync(userId, householdId))
            throw new DomainException("Lar não encontrado ou sem permissão.");

        return await _transactionQueries.GetHouseholdSummaryByProfileAsync(householdId, month, year);
    }

    public async Task<IReadOnlyList<ReceivedPendingInviteDto>> ListMyPendingInvitesAsync(Guid userId)
    {
        var user = await _users.GetByIdAsync(userId);
        if (user is null)
            throw new DomainException("Usuário não encontrado.");

        var normalized = user.Email.Value.Trim().ToLowerInvariant();
        var invites = await _invites.GetPendingInvitesByInviteeEmailAsync(normalized);
        if (invites.Count == 0)
            return [];

        var householdIds = invites.Select(i => i.HouseholdId).Distinct().ToArray();
        var households = await _households.GetByIdsAsync(householdIds);
        var householdNames = households.ToDictionary(h => h.Id, h => h.Name);

        var inviterIds = invites.Select(i => i.InviterUserId).Distinct().ToArray();
        var inviterUsers = await _users.GetByIdsAsync(inviterIds);
        var inviterNames = inviterUsers.ToDictionary(u => u.Id, u => u.Name);

        var now = DateTime.UtcNow;
        var result = new List<ReceivedPendingInviteDto>(invites.Count);
        foreach (var inv in invites)
        {
            householdNames.TryGetValue(inv.HouseholdId, out var householdName);
            inviterNames.TryGetValue(inv.InviterUserId, out var inviterName);
            result.Add(new ReceivedPendingInviteDto(
                inv.Id,
                inv.HouseholdId,
                householdName ?? "(lar)",
                inv.Token,
                inv.ExpiresAt,
                inv.CreatedAt,
                now > inv.ExpiresAt,
                inv.InviterUserId,
                inviterName));
        }

        return result;
    }

    public async Task<IReadOnlyList<SentPendingInviteDto>> ListPendingInvitesForHouseholdAsync(
        Guid userId,
        Guid householdId)
    {
        if (!await _memberships.IsMemberAsync(userId, householdId))
            throw new DomainException("Lar não encontrado ou sem permissão.");

        var invites = await _invites.GetPendingInvitesByHouseholdAsync(householdId);
        if (invites.Count == 0)
            return [];

        var inviterIds = invites.Select(i => i.InviterUserId).Distinct().ToList();
        var inviterUsers = await _users.GetByIdsAsync(inviterIds);
        var inviterById = inviterUsers.ToDictionary(u => u.Id, u => u.Name);

        var now = DateTime.UtcNow;
        var result = new List<SentPendingInviteDto>(invites.Count);
        foreach (var inv in invites)
        {
            inviterById.TryGetValue(inv.InviterUserId, out var inviterName);
            result.Add(new SentPendingInviteDto(
                inv.Id,
                inv.InviteeEmailNormalized,
                inv.ExpiresAt,
                inv.CreatedAt,
                now > inv.ExpiresAt,
                inv.InviterUserId,
                inviterName));
        }

        return result;
    }

    /// <summary>Membros com papel Member (entrada típica após aceitar convite).</summary>
    public async Task<IReadOnlyList<HouseholdMemberDto>> ListInvitedMembersAsync(Guid userId, Guid householdId)
    {
        if (!await _memberships.IsMemberAsync(userId, householdId))
            throw new DomainException("Lar não encontrado ou sem permissão.");

        var memberships = await _memberships.GetMembersByHouseholdAsync(householdId);
        var memberRows = memberships.Where(m => m.Role == HouseholdRole.Member).ToList();
        if (memberRows.Count == 0)
            return [];

        var userIds = memberRows.Select(m => m.UserId).Distinct().ToList();
        var users = await _users.GetByIdsAsync(userIds);
        var byId = users.ToDictionary(u => u.Id);

        var result = new List<HouseholdMemberDto>(memberRows.Count);
        foreach (var m in memberRows)
        {
            if (!byId.TryGetValue(m.UserId, out var u))
                continue;
            result.Add(new HouseholdMemberDto(
                u.Id,
                u.Name,
                u.Email.Value,
                HouseholdRole.Member.ToString(),
                m.JoinedAt));
        }

        return result;
    }
}
