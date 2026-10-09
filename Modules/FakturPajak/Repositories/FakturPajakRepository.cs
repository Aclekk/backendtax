using Microsoft.EntityFrameworkCore;
using TaxRefund.Api.Shared.Database;
using TaxRefund.Api.Shared.Database.Entities;

namespace TaxRefund.Api.Modules.FakturPajak.Repositories;

public class FakturPajakRepository(TaxRefundDbContext context) : IFakturPajakRepository
{
    private readonly TaxRefundDbContext _context = context;

    public async Task<TFakturPajak> AddAsync(TFakturPajak fakturPajak, List<TFakturPajakDetail> details)
    {
        await using var tx = await _context.Database.BeginTransactionAsync();

        var existing = await _context.TFakturPajaks
            .Include(f => f.TFakturPajakDetails)
            .FirstOrDefaultAsync(f => f.InvoiceId == fakturPajak.InvoiceId);

        TFakturPajak saved;
        if (existing is not null)
        {
            existing.NomorFakturPajak = fakturPajak.NomorFakturPajak;
            existing.FilePath = fakturPajak.FilePath;
            existing.FileName = fakturPajak.FileName;
            existing.UploadedBy = fakturPajak.UploadedBy;
            existing.UploadedAt = DateTime.Now;
            existing.UpdatedAt = DateTime.Now;
            existing.UpdatedBy = fakturPajak.UploadedBy;

            if (existing.TFakturPajakDetails.Count > 0)
            {
                _context.TFakturPajakDetails.RemoveRange(existing.TFakturPajakDetails);
            }

            await _context.SaveChangesAsync();
            saved = existing;
        }
        else
        {
            fakturPajak.CreatedAt = DateTime.Now;
            fakturPajak.CreatedBy = fakturPajak.UploadedBy;
            _context.TFakturPajaks.Add(fakturPajak);
            await _context.SaveChangesAsync();
            saved = fakturPajak;
        }

        foreach (var detail in details)
        {
            detail.FakturPajakId = saved.Id;
            detail.CreatedAt = DateTime.Now;
        }

        _context.TFakturPajakDetails.AddRange(details);
        await _context.SaveChangesAsync();
        await tx.CommitAsync();

        return saved;
    }

    public async Task<TFakturPajak?> GetByInvoiceIdAsync(int invoiceId)
    {
        return await _context.TFakturPajaks
            .Include(f => f.Invoice)
            .FirstOrDefaultAsync(f => f.InvoiceId == invoiceId);
    }

    public async Task<TFakturPajak?> GetByIdAsync(int id)
    {
        return await _context.TFakturPajaks
            .Include(f => f.Invoice)
            .FirstOrDefaultAsync(f => f.Id == id);
    }

    public async Task<List<TFakturPajak>> GetAllWithInvoiceAsync()
    {
        return await _context.TFakturPajaks
            .Include(f => f.Invoice)
            .OrderByDescending(f => f.UploadedAt)
            .ToListAsync();
    }

    public async Task AddEmailLogAsync(TEmailLog log)
    {
        _context.TEmailLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateInvoiceStatusAsync(int invoiceId, InvoiceStatus status)
    {
        var invoice = await _context.TInvoices.FirstOrDefaultAsync(i => i.Id == invoiceId);
        if (invoice is not null)
        {
            invoice.Status = status;
            await _context.SaveChangesAsync();
        }
    }

    public async Task SetEmailSentAtAsync(int fakturPajakId, DateTime sentAt)
    {
        var fp = await _context.TFakturPajaks.FirstOrDefaultAsync(f => f.Id == fakturPajakId);
        if (fp is not null)
        {
            fp.EmailSentAt = sentAt;
            await _context.SaveChangesAsync();
        }
    }

}