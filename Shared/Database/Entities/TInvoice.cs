using System;
using System.Collections.Generic;

namespace TaxRefund.Api.Shared.Database.Entities;

public partial class TInvoice
{
    public int Id { get; set; }

    public int? Baris { get; set; }

    public string NoInvoice { get; set; } = null!;

    public string StoreCode { get; set; } = string.Empty;

    public DateOnly TanggalFaktur { get; set; }

    public string JenisFaktur { get; set; } = null!;

    public string KodeTransaksi { get; set; } = null!;

    public string? KeteranganTambahan { get; set; }

    public string? DokumenPendukung { get; set; }

    public string? PeriodDokPendukung { get; set; }

    public string? Referensi { get; set; }

    public string? CapFasilitas { get; set; }

    public string IdTkuPenjual { get; set; } = null!;

    public string NpwpNikPembeli { get; set; } = null!;

    public string JenisIdPembeli { get; set; } = null!;

    public string NegaraPembeli { get; set; } = null!;

    public string NomorDokumenPembeli { get; set; } = null!;

    public string NamaPembeli { get; set; } = null!;

    public string AlamatPembeli { get; set; } = null!;

  public string EmailPembeli { get; set; } = null!;

    public string IdTkuPembeli { get; set; } = null!;

    public InvoiceStatus Status { get; set; }

    public string CreatedBy { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
    public DateTime? CoreTaxFailureEmailSentAt { get; set; }

    // New nullable FK to customer/traveler
    public int? CustomerId { get; set; }
    public virtual TTraveler? Customer { get; set; }

    public virtual TFakturPajak? TFakturPajak { get; set; }

    public virtual ICollection<TInvoiceDownload> TInvoiceDownloads { get; set; } = new List<TInvoiceDownload>();
}
