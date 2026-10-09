using Microsoft.EntityFrameworkCore;
using TaxRefund.Api.Shared.Database;
using TaxRefund.Api.Shared.Database.Entities;

namespace TaxRefund.Api.Modules.Negara.Repositories;

public class NegaraRepository(TaxRefundDbContext context) : INegaraRepository
{
    private readonly TaxRefundDbContext _context = context;

    public async Task<List<TNegara>> GetAllAsync()
    {
        return await _context.TNegaras
            .Where(x => x.IsActive)
            .OrderBy(x => x.Nama)
            .ToListAsync();
    }
}