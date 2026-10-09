using TaxRefund.Api.Modules.Invoice.DTOs;

namespace TaxRefund.Api.Modules.Invoice.Services;

public interface IInvoiceService
{
    Task<InvoiceResponse> CreateInvoiceAsync(CreateInvoiceRequest request, string createdBy);
    Task<PagedResponse<InvoiceResponse>> GetInvoicesAsync(int page, int pageSize);
    Task<InvoiceResponse> DownloadInvoiceAsync(int id, string downloadedBy);
    Task<CegidPreviewResponse> PreviewCegidAsync(string storeCode, string noInvoice);
    Task<(byte[] FileBytes, string FileName)> DownloadInvoiceXlsxAsync(int id, string downloadedBy);
}
