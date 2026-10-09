using System;
using System.Collections.Generic;

namespace TaxRefund.Api.Shared.Database.Entities;

public partial class TFakturPajak
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public string NomorFakturPajak { get; set; } = null!;
    public string FilePath { get; set; } = null!;
    public string FileName { get; set; } = null!;
    public string UploadedBy { get; set; } = null!;
    public DateTime UploadedAt { get; set; }
    public DateTime? EmailSentAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    public virtual TInvoice Invoice { get; set; } = null!;
    public virtual ICollection<TEmailLog> TEmailLogs { get; set; } = new List<TEmailLog>();
    public virtual ICollection<TFakturPajakDetail> TFakturPajakDetails { get; set; } = new List<TFakturPajakDetail>();
}