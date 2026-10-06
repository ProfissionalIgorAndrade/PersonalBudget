using PersonalBudget.Application.DTOs.Simulator;
using PersonalBudget.Application.Interfaces;

namespace PersonalBudget.Application.Services.Simulator;

/// <summary>
/// Orquestra a projeção: valida o pedido, lê saldo de partida e totais agregados e entrega
/// tudo à matemática pura (<see cref="ProjectionBuilder"/>). Somente leitura.
/// </summary>
public class ProjectionService : IProjectionService
{
    private readonly ITransactionQueryRepository _queryRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly IHouseholdMemberProfileRepository _profileRepository;

    public ProjectionService(
        ITransactionQueryRepository queryRepository,
        ITransactionRepository transactionRepository,
        IAccountRepository accountRepository,
        IHouseholdMemberProfileRepository profileRepository)
    {
        _queryRepository = queryRepository;
        _transactionRepository = transactionRepository;
        _accountRepository = accountRepository;
        _profileRepository = profileRepository;
    }

    public async Task<ProjectionResponse> ProjectAsync(ProjectionCommand command)
    {
        // Sem "today" no pedido: data do servidor em UTC-3 (horário de Brasília).
        var request = ProjectionValidator.Validate(command, DateTime.UtcNow.AddHours(-3));

        var accounts = (await _accountRepository.GetActiveCheckingByHouseholdIdAsync(command.HouseholdId)).ToList();
        var profiles = await _profileRepository.GetByHouseholdAsync(command.HouseholdId);
        var profileNames = profiles.ToDictionary(p => p.Id, p => p.DisplayName);

        var balances = await _transactionRepository.GetBalancesByAccountIdsUntilAsync(
            accounts.Select(a => a.Id), request.Today);

        var openingAccounts = accounts
            .Select(a =>
            {
                string? memberName = a.MemberProfileId.HasValue
                    && profileNames.TryGetValue(a.MemberProfileId.Value, out var n) ? n : null;
                return new OpeningAccountDto(a.Id, AccountDisplayName.Build(a, memberName), balances.GetValueOrDefault(a.Id, 0m));
            })
            .ToList();

        var first = request.Reference.AddMonths(-ProjectionBuilder.LookbackMonths);
        var last = request.Reference.AddMonths(request.Months - 1);

        var rows = await _queryRepository.GetProjectionFlowAsync(
            command.HouseholdId, first.Month, first.Year, last.Month, last.Year, request.Today);

        return ProjectionBuilder.Build(
            request.Reference, request.Today, request.Months, openingAccounts, rows, request.Impacts);
    }
}
