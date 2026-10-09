using System;
using System.Collections.Generic;

namespace TaxRefund.Api.Shared.Database.Entities;

public partial class TEmailLog
{
    public int Id { get; set; }
    public int FakturPajakId { get; set; }
    public string? ErrorMessage { get; set; }
    public EmailLogStatus Status { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public virtual TFakturPajak FakturPajak { get; set; } = null!;
}