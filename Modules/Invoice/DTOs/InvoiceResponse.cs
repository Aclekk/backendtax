namespace TaxRefund.Api.Modules.Invoice.DTOs;

public class InvoiceResponse
{
    public int Id { get; set; }
    public int? Baris { get; set; }
    public string NoInvoice { get; set; } = null!;
    public string StoreCode { get; set; } = string.Empty;
    public string AlamatPembeli { get; set; } = string.Empty;
    public DateOnly TanggalFaktur { get; set; }
    public string JenisFaktur { get; set; } = null!;
    public string KodeTransaksi { get; set; } = null!;
    public string? KeteranganTambahan { get; set; }
    public string? Referensi { get; set; }
    public string NamaPembeli { get; set; } = null!;
    public string NegaraPembeli { get; set; } = null!;
    public string NomorDokumenPembeli { get; set; } = string.Empty;
    public string EmailPembeli { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string CreatedBy { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? CoreTaxFailureEmailSentAt { get; set; }
}

public class PagedResponse<T>
{
    public List<T> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
}