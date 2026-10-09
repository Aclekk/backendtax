using TaxRefund.Api.Shared.Database.Entities;

namespace TaxRefund.Api.Modules.Negara.Repositories;

public interface INegaraRepository
{
    Task<List<TNegara>> GetAllAsync();
}