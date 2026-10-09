using Microsoft.AspNetCore.Http;
using TaxRefund.Api.Modules.FakturPajak.DTOs;

namespace TaxRefund.Api.Modules.FakturPajak.Services;

public interface IFakturPajakService
{
    Task<FakturPajakResponse> UploadAsync(int invoiceId, UploadFakturPajakRequest request, string uploadedBy);
    Task<List<FakturPajakResponse>> GetAllAsync();
    Task<FakturPajakResponse> ResendEmailAsync(int fakturPajakId, string resentBy);
    Task SendCoreTaxFailureEmailAsync(int invoiceId, string sentBy);
}