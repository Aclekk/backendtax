using TaxRefund.Api.Modules.Negara.DTOs;
using TaxRefund.Api.Modules.Negara.Repositories;

namespace TaxRefund.Api.Modules.Negara.Services;

public class NegaraService(INegaraRepository negaraRepository) : INegaraService
{
    private readonly INegaraRepository _negaraRepository = negaraRepository;

    public async Task<List<NegaraResponse>> GetAllAsync()
    {
        var negaraList = await _negaraRepository.GetAllAsync();

        return negaraList.Select(x => new NegaraResponse
        {
            Id = x.Id,
            Kode = x.Kode,
            Nama = x.Nama
        }).ToList();
    }
}