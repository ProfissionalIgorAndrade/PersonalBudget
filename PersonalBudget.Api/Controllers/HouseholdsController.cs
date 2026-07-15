using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalBudget.Api.Contracts;
using PersonalBudget.Application.Interfaces;

namespace PersonalBudget.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/households")]
public class HouseholdsController : ControllerBase
{
    private readonly IHouseholdService _householdService;

    public HouseholdsController(IHouseholdService householdService)
    {
        _householdService = householdService;
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var userId = UserContext.GetUserId(User);
        var list = await _householdService.ListForUserAsync(userId);
        return Ok(ApiResponse<object>.Ok(list));
    }

    /// <summary>Convites pendentes cujo destinatário é o seu e-mail (inclui token para aceitar).</summary>
    [HttpGet("invites/me")]
    public async Task<IActionResult> ListMyPendingInvites()
    {
        var userId = UserContext.GetUserId(User);
        var list = await _householdService.ListMyPendingInvitesAsync(userId);
        return Ok(ApiResponse<object>.Ok(list));
    }

    /// <summary>Convites pendentes enviados por este lar (sem token).</summary>
    [HttpGet("{householdId:guid}/invites/pending")]
    public async Task<IActionResult> ListPendingInvitesForHousehold(Guid householdId)
    {
        var userId = UserContext.GetUserId(User);
        var list = await _householdService.ListPendingInvitesForHouseholdAsync(userId, householdId);
        return Ok(ApiResponse<object>.Ok(list));
    }

    /// <summary>Membros que entraram como convidados (papel Member após aceitar convite).</summary>
    [HttpGet("{householdId:guid}/members/invited")]
    public async Task<IActionResult> ListInvitedMembers(Guid householdId)
    {
        var userId = UserContext.GetUserId(User);
        var list = await _householdService.ListInvitedMembersAsync(userId, householdId);
        return Ok(ApiResponse<object>.Ok(list));
    }

    /// <summary>Perfis de correspondente (Igor, Andreza, Família, etc.) para atribuição de lançamentos.</summary>
    [HttpGet("{householdId:guid}/profiles")]
    public async Task<IActionResult> GetProfiles(Guid householdId)
    {
        var userId = UserContext.GetUserId(User);
        var profiles = await _householdService.GetProfilesAsync(userId, householdId);
        return Ok(ApiResponse<object>.Ok(profiles));
    }

    /// <summary>Adiciona um correspondente compartilhado (ex.: rótulo para gastos do lar, sem novo usuário no sistema).</summary>
    [HttpPost("{householdId:guid}/profiles")]
    public async Task<IActionResult> CreateProfile(Guid householdId, [FromBody] CreateHouseholdMemberProfileRequest request)
    {
        var userId = UserContext.GetUserId(User);
        var created = await _householdService.CreateJointProfileAsync(userId, householdId, request.DisplayName, request.Emoji, request.Color);
        return CreatedAtAction(nameof(GetProfiles), new { householdId },
            ApiResponse<object>.Ok(created, "Perfil de correspondente criado."));
    }

    /// <summary>Atualiza nome e aparência (emoji, cor) de um perfil de correspondente.</summary>
    [HttpPut("{householdId:guid}/profiles/{profileId:guid}")]
    public async Task<IActionResult> UpdateProfile(Guid householdId, Guid profileId, [FromBody] UpdateHouseholdMemberProfileRequest request)
    {
        var userId = UserContext.GetUserId(User);
        var updated = await _householdService.UpdateProfileAsync(userId, householdId, profileId, request.DisplayName, request.Emoji, request.Color);
        return Ok(ApiResponse<object>.Ok(updated));
    }

    /// <summary>
    /// Remove um correspondente: migra todos os lançamentos para outro perfil; contas e cartões só quando ambos forem perfis vinculados a usuários diferentes (titularidade).
    /// </summary>
    [HttpPost("{householdId:guid}/profiles/delete")]
    public async Task<IActionResult> DeleteProfile(Guid householdId, [FromBody] DeleteHouseholdMemberProfileRequest request)
    {
        var userId = UserContext.GetUserId(User);
        await _householdService.DeleteProfileAndMergeAsync(
            userId,
            householdId,
            request.RemoveProfileId,
            request.MergeIntoProfileId);
        return Ok(ApiResponse<object?>.Ok(null, "Perfil removido e dados migrados."));
    }

    /// <summary>Resumo por pessoa: totais de saídas e ganhos por correspondente no mês/ano.</summary>
    [HttpGet("{householdId:guid}/summary/by-profile")]
    public async Task<IActionResult> GetSummaryByProfile(
        Guid householdId,
        [FromQuery] int month,
        [FromQuery] int year)
    {
        var userId = UserContext.GetUserId(User);
        var rows = await _householdService.GetSummaryByProfileAsync(userId, householdId, month, year);
        return Ok(ApiResponse<object>.Ok(rows));
    }

    [HttpPost("invites")]
    public async Task<IActionResult> CreateInvite([FromBody] CreateHouseholdInviteRequest request)
    {
        var userId = UserContext.GetUserId(User);
        var token = await _householdService.CreateInviteAsync(userId, request.HouseholdId, request.InviteeEmail);
        return Ok(ApiResponse<object>.Ok(new { Token = token }, "Convite criado. Envie o token para o convidado aceitar em POST /api/households/invites/accept."));
    }

    [HttpPost("invites/accept")]
    public async Task<IActionResult> AcceptInvite([FromBody] AcceptInviteRequest request)
    {
        var userId = UserContext.GetUserId(User);
        await _householdService.AcceptInviteAsync(userId, request.Token);
        return Ok(ApiResponse<object?>.Ok(null, "Convite aceito. Você passa a ver os dados deste lar."));
    }
}
