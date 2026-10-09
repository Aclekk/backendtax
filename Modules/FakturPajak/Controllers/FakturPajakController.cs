using Microsoft.AspNetCore.Mvc;
using TaxRefund.Api.Modules.FakturPajak.DTOs;
using TaxRefund.Api.Modules.FakturPajak.Services;
using TaxRefund.Api.Shared.Common;

namespace TaxRefund.Api.Modules.FakturPajak.Controllers;

[ApiController]
[Route("api/faktur-pajak")]
public class FakturPajakController(IFakturPajakService fakturPajakService) : ControllerBase
{
    private readonly IFakturPajakService _fakturPajakService = fakturPajakService;

    [HttpPost("{invoiceId}/upload")]
    public async Task<IActionResult> Upload(int invoiceId, [FromForm] UploadFakturPajakRequest request)
    {
        var uploadedBy = "system";

        try
        {
            var result = await _fakturPajakService.UploadAsync(invoiceId, request, uploadedBy);

            var message = result.InvoiceStatus == "Sent"
                ? "Faktur Pajak berhasil diupload dan email terkirim"
                : "Faktur Pajak berhasil diupload, namun pengiriman email gagal";

            return Ok(ApiResponse<FakturPajakResponse>.Ok(result, message));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<object>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            var detail = ex.InnerException?.Message ?? ex.Message;
            return StatusCode(500, ApiResponse<object>.Fail(detail));
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _fakturPajakService.GetAllAsync();
        return Ok(ApiResponse<List<FakturPajakResponse>>.Ok(result));
    }

    [HttpPost("{id}/resend-email")]
    public async Task<IActionResult> ResendEmail(int id)
    {
        var resentBy = "system";

        try
        {
            var result = await _fakturPajakService.ResendEmailAsync(id, resentBy);

            var message = result.InvoiceStatus == "Sent"
                ? "Email berhasil dikirim ulang"
                : "Pengiriman ulang email gagal";

            return Ok(ApiResponse<FakturPajakResponse>.Ok(result, message));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message));
        }
    }

    [HttpPost("{invoiceId}/coretax-failed-email")]
    public async Task<IActionResult> SendCoreTaxFailureEmail(int invoiceId)
    {
        try
        {
            await _fakturPajakService.SendCoreTaxFailureEmailAsync(invoiceId, "system");
            return Ok(ApiResponse<object>.Ok(
                new { invoiceId },
                "Email notifikasi kegagalan Coretax berhasil dikirim."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<object>.Fail(ex.Message));
        }
    }
}