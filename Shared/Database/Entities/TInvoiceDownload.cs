using System;
using System.Collections.Generic;

namespace TaxRefund.Api.Shared.Database.Entities;

public partial class TInvoiceDownload
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public string CreatedBy { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public virtual TInvoice Invoice { get; set; } = null!;
}