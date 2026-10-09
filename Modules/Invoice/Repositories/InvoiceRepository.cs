using Microsoft.EntityFrameworkCore;
using TaxRefund.Api.Shared.Database;
using TaxRefund.Api.Shared.Database.Entities;

namespace TaxRefund.Api.Modules.Invoice.Repositories;

using Microsoft.EntityFrameworkCore;
public class InvoiceRepository(TaxRefundDbContext context) : IInvoiceRepository
{
    private readonly TaxRefundDbContext _context = context;

    public async Task<TInvoice> AddAsync(TInvoice invoice)
    {
        _context.TInvoices.Add(invoice);
        await _context.SaveChangesAsync();
        return invoice;
    }

    public async Task<(List<TInvoice> Items, int TotalCount)> GetPagedAsync(int page, int pageSize)
    {
        var query = _context.TInvoices.OrderByDescending(i => i.CreatedAt);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<TInvoice?> GetByIdAsync(int id)
    {
        return await _context.TInvoices.FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task UpdateAsync(TInvoice invoice)
    {
        _context.TInvoices.Update(invoice);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> NoInvoiceExistsAsync(string noInvoice, string storeCode)
    {
        return await _context.TInvoices.AnyAsync(i => i.NoInvoice == noInvoice && i.StoreCode == storeCode);
    }

    public async Task MarkAsDownloadedAsync(TInvoice invoice, string downloadedBy)
    {
        var downloadedAt = DateTime.Now;
        await using var transaction = await _context.Database.BeginTransactionAsync();

        await _context.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO t_invoice_download (invoice_id, created_by, created_at)
            VALUES ({invoice.Id}, {downloadedBy}, {downloadedAt})
            ON CONFLICT (invoice_id) DO NOTHING");

        var updated = await _context.TInvoices
            .Where(item => item.Id == invoice.Id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.Status, InvoiceStatus.Downloaded));

        if (updated != 1)
        {
            throw new KeyNotFoundException($"Invoice dengan id {invoice.Id} tidak ditemukan saat memperbarui status download.");
        }

        await transaction.CommitAsync();
        invoice.Status = InvoiceStatus.Downloaded;
    }

    // BARU — dipakai PendingInvoiceReminderService (scheduler jam 6 sore)
    public async Task<int> CountPendingAsync()
    {
        return await _context.TInvoices
            .Where(i => i.Status == InvoiceStatus.New || i.Status == InvoiceStatus.Downloaded)
            .CountAsync();
    }
}
