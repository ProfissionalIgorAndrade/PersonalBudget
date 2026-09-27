using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalBudget.Api.Contracts;
using PersonalBudget.Application.Interfaces;

namespace PersonalBudget.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/financial-calendar")]
public class FinancialCalendarController : ControllerBase
{
    private readonly IFinancialCalendarService _calendarService;
    private readonly IActiveHouseholdResolver  _householdResolver;

    public FinancialCalendarController(
        IFinancialCalendarService calendarService,
        IActiveHouseholdResolver  householdResolver)
    {
        _calendarService   = calendarService;
        _householdResolver = householdResolver;
    }

    /// <summary>
    /// Retorna o calendário financeiro do período [from, to].
    ///
    /// Parâmetros: from e to no formato YYYY-MM-DD (ISO 8601).
    /// Intervalo máximo: 62 dias (≈ 2 meses).
    ///
    /// A resposta inclui:
    /// - openingBalance: saldo atual consolidado das contas correntes.
    /// - days: um item por dia do período com lançamentos, faturas e saldo projetado.
    /// - projectedBalance: acumulativo a partir de hoje; null para dias passados.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetCalendar(
        [FromQuery] string from,
        [FromQuery] string to)
    {
        if (!DateTime.TryParse(from, out var fromDate))
            return BadRequest(ApiResponse<object>.Fail("Parâmetro 'from' inválido. Use o formato YYYY-MM-DD."));

        if (!DateTime.TryParse(to, out var toDate))
            return BadRequest(ApiResponse<object>.Fail("Parâmetro 'to' inválido. Use o formato YYYY-MM-DD."));

        if (fromDate > toDate)
            return BadRequest(ApiResponse<object>.Fail("'from' deve ser anterior ou igual a 'to'."));

        if ((toDate - fromDate).TotalDays > 62)
            return BadRequest(ApiResponse<object>.Fail("O intervalo máximo é de 62 dias."));

        var userId      = UserContext.GetUserId(User);
        var householdId = await _householdResolver.ResolveAsync(userId, HouseholdHttp.TryGetHouseholdIdHeader(Request));
        var result      = await _calendarService.GetCalendarAsync(householdId, fromDate, toDate);

        return Ok(ApiResponse<object>.Ok(result));
    }
}
