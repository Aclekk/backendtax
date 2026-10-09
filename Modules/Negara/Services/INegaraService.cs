using TaxRefund.Api.Modules.Negara.DTOs;

namespace TaxRefund.Api.Modules.Negara.Services;

public interface INegaraService
{
    Task<List<NegaraResponse>> GetAllAsync();
}