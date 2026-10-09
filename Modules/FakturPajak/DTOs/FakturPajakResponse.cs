namespace TaxRefund.Api.Modules.FakturPajak.DTOs;

public class FakturPajakResponse
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public string NoInvoice { get; set; } = null!;
    public string NamaPembeli { get; set; } = null!;
    public string NegaraPembeli { get; set; } = null!;
    public string NomorFakturPajak { get; set; } = null!;
    public string FileName { get; set; } = null!;
    public string UploadedBy { get; set; } = null!;
    public DateTime UploadedAt { get; set; }
    public DateTime? EmailSentAt { get; set; }
    public string InvoiceStatus { get; set; } = null!;
}