using Microsoft.AspNetCore.Mvc;
using TaxRefund.Api.Modules.Negara.DTOs;
using TaxRefund.Api.Modules.Negara.Services;
using TaxRefund.Api.Shared.Common;

namespace TaxRefund.Api.Modules.Negara.Controllers;

[ApiController]
[Route("api/negara")]
public class NegaraController(INegaraService negaraService) : ControllerBase
{
    private readonly INegaraService _negaraService = negaraService;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _negaraService.GetAllAsync();

        return Ok(ApiResponse<List<NegaraResponse>>.Ok(result));
    }
}