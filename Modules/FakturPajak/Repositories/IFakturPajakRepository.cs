using TaxRefund.Api.Shared.Database.Entities;

namespace TaxRefund.Api.Modules.FakturPajak.Repositories;

public interface IFakturPajakRepository
{
    Task<TFakturPajak> AddAsync(TFakturPajak fakturPajak, List<TFakturPajakDetail> details);
    Task<TFakturPajak?> GetByInvoiceIdAsync(int invoiceId);
    Task<TFakturPajak?> GetByIdAsync(int id);
    Task<List<TFakturPajak>> GetAllWithInvoiceAsync();
    Task AddEmailLogAsync(TEmailLog log);
    Task UpdateInvoiceStatusAsync(int invoiceId, InvoiceStatus status);
    Task SetEmailSentAtAsync(int fakturPajakId, DateTime sentAt);
}