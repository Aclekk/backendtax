using Microsoft.AspNetCore.Mvc;
using TaxRefund.Api.Modules.Traveler.DTOs;
using TaxRefund.Api.Modules.Traveler.Services;
using TaxRefund.Api.Shared.Common;

namespace TaxRefund.Api.Modules.Traveler.Controllers;

[ApiController]
[Route("api/tax-refund-customer")]
public class CustomerController(ITravelerService travelerService) : ControllerBase
{
    private readonly ITravelerService _travelerService = travelerService;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? passportNumber = null)
    {
        var result = await _travelerService.GetTravelersAsync(passportNumber);
        return Ok(ApiResponse<List<TravelerResponse>>.Ok(result));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _travelerService.GetTravelerAsync(id);
        if (result is null)
        {
            return NotFound(ApiResponse<object>.Fail($"Customer dengan ID {id} tidak ditemukan."));
        }

        return Ok(ApiResponse<TravelerDetailResponse>.Ok(result));
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistoryList()
    {
        var result = await _travelerService.GetHistoryListAsync();
        return Ok(ApiResponse<List<TravelerHistoryListResponse>>.Ok(result, "History customer berhasil dimuat."));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTravelerRequest request)
    {
        try
        {
            var result = await _travelerService.CreateTravelerAsync(request);
            return Ok(ApiResponse<TravelerResponse>.Ok(result, "Customer berhasil dibuat."));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

}
