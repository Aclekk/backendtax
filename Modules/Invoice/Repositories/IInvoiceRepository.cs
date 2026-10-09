using TaxRefund.Api.Shared.Database.Entities;

namespace TaxRefund.Api.Modules.Invoice.Repositories;

public interface IInvoiceRepository
{
    Task<TInvoice> AddAsync(TInvoice invoice);
    Task<(List<TInvoice> Items, int TotalCount)> GetPagedAsync(int page, int pageSize);
    Task<TInvoice?> GetByIdAsync(int id);
    Task UpdateAsync(TInvoice invoice);
    Task<bool> NoInvoiceExistsAsync(string noInvoice, string storeCode);
    Task MarkAsDownloadedAsync(TInvoice invoice, string downloadedBy);
    Task<int> CountPendingAsync();   // ← BARU
}