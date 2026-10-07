using Microsoft.EntityFrameworkCore;

public class SimulationRepository : ISimulationRepository
{
    private readonly AppDbContext _context;

    public SimulationRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Simulation>> GetByHouseholdAsync(Guid householdId)
    {
        return await _context.Simulations
            .AsNoTracking()
            .Where(s => s.HouseholdId == householdId)
            .OrderBy(s => s.CreatedAt)
            .ThenBy(s => s.Id)
            .ToListAsync();
    }

    public async Task<Simulation?> GetByIdAsync(Guid id)
    {
        return await _context.Simulations
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<int> CountByOwnerAsync(Guid householdId, Guid ownerUserId)
    {
        return await _context.Simulations
            .CountAsync(s => s.HouseholdId == householdId && s.OwnerUserId == ownerUserId);
    }

    public async Task AddAsync(Simulation simulation)
    {
        _context.Simulations.Add(simulation);
        await SaveChangesAsync();
    }

    public async Task AddRangeAsync(IReadOnlyList<Simulation> simulations)
    {
        if (simulations.Count == 0)
            return;

        _context.Simulations.AddRange(simulations);
        await SaveChangesAsync();
    }

    public async Task UpdateAsync(Simulation simulation)
    {
        _context.Simulations.Update(simulation);
        await SaveChangesAsync();
    }

    public async Task RemoveAsync(Simulation simulation)
    {
        _context.Simulations.Remove(simulation);
        await SaveChangesAsync();
    }

    public async Task<int> RemoveByOwnerAsync(Guid householdId, Guid ownerUserId)
    {
        var mine = await _context.Simulations
            .Where(s => s.HouseholdId == householdId && s.OwnerUserId == ownerUserId)
            .ToListAsync();

        if (mine.Count == 0)
            return 0;

        _context.Simulations.RemoveRange(mine);
        await SaveChangesAsync();
        return mine.Count;
    }

    public async Task<IReadOnlyList<Simulation>> GetAllByHouseholdAsync(Guid householdId)
    {
        return await _context.Simulations
            .Where(s => s.HouseholdId == householdId)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Simulation>> GetAllByHouseholdAndOwnerAsync(Guid householdId, Guid ownerUserId)
    {
        return await _context.Simulations
            .Where(s => s.HouseholdId == householdId && s.OwnerUserId == ownerUserId)
            .ToListAsync();
    }

    public async Task BulkUpdateAsync(IReadOnlyList<Simulation> simulations)
    {
        if (simulations.Count == 0)
            return;

        _context.Simulations.UpdateRange(simulations);
        await SaveChangesAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
