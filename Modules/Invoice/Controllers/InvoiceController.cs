using Microsoft.AspNetCore.Mvc;
using TaxRefund.Api.Modules.Invoice.DTOs;
using TaxRefund.Api.Modules.Invoice.Services;
using TaxRefund.Api.Shared.Common;

namespace TaxRefund.Api.Modules.Invoice.Controllers;

[ApiController]
[Route("api/invoice")]
public class InvoiceController(IInvoiceService invoiceService) : ControllerBase
{
    private readonly IInvoiceService _invoiceService = invoiceService;
[HttpGet("preview-cegid")]
public async Task<IActionResult> PreviewCegid([FromQuery] string? storeCode, [FromQuery] string? noInvoice)
{
    if (string.IsNullOrWhiteSpace(noInvoice))
    {
        return BadRequest(ApiResponse<object>.Fail("noInvoice wajib diisi."));
    }

    try
    {
        var resolvedStoreCode = string.IsNullOrWhiteSpace(storeCode) ? string.Empty : storeCode.Trim();
        var result = await _invoiceService.PreviewCegidAsync(resolvedStoreCode, noInvoice.Trim());
        return Ok(ApiResponse<CegidPreviewResponse>.Ok(result));
    }
    catch (KeyNotFoundException ex)
    {
        return NotFound(ApiResponse<object>.Fail(ex.Message));
    }
    catch (Exception ex)
    {
        return StatusCode(500, ApiResponse<object>.Fail(ex.Message));
    }
}
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateInvoiceRequest request)
    {
        var createdBy = "system";

        try
        {
            var result = await _invoiceService.CreateInvoiceAsync(request, createdBy);
            return Ok(ApiResponse<InvoiceResponse>.Ok(result, "Invoice berhasil dibuat"));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<object>.Fail(ex.Message));
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _invoiceService.GetInvoicesAsync(page, pageSize);
        return Ok(ApiResponse<PagedResponse<InvoiceResponse>>.Ok(result));
    }

    [HttpGet("{id}/download")]
    [HttpPost("{id}/download")]
    public async Task<IActionResult> Download(int id)
    {
        var downloadedBy = "system";

        try
        {
            var (fileBytes, fileName) = await _invoiceService.DownloadInvoiceXlsxAsync(id, downloadedBy);
            return File(
                fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
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

}
