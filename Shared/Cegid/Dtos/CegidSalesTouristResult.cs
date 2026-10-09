namespace TaxRefund.Api.Shared.Cegid.Dtos;

public class CegidSalesTouristResult
{
    // --- Header ---
    public string StoreCode { get; set; } = string.Empty;
    public string StoreName { get; set; } = string.Empty;
    public string? StoreCity { get; set; }      // BARU — ET_VILLE
    public string? Provinsi { get; set; }       // BARU — RG_LIBELLE
    public string ReceiptNo { get; set; } = string.Empty;
    public DateTime TransDate { get; set; }
    public string NomorMember { get; set; } = string.Empty;
    public string NamaMember { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Address { get; set; }

    // --- Detail ---
    public string NamaBrg { get; set; } = string.Empty;
    public string? Brand { get; set; }          // BARU — CHOIXCOD type 'GDM'
    public string? Gender { get; set; }         // BARU — CHOIXCOD type 'FN1'
    public string? Category { get; set; }       // BARU — CHOIXCOD type 'FN4', dipakai untuk lookup KodeBarangJasa
    public string HargaSatuan { get; set; } = string.Empty;
    public string JumlahBrgJasa { get; set; } = string.Empty;
    public string HargaTotal { get; set; } = string.Empty;
    public string TotalDiskon { get; set; } = string.Empty;
    public string Dpp { get; set; } = string.Empty;
    public string DppNilaiLain { get; set; } = string.Empty;
    public string Pct { get; set; } = string.Empty;
    public string Ppn { get; set; } = string.Empty;
}