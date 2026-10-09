namespace TaxRefund.Api.Modules.FakturPajak.DTOs;

public class DetailFakturItemRequest
{
    public int Baris { get; set; }
    public string BarangJasa { get; set; } = null!;
    public string KodeBarangJasa { get; set; } = null!;
    public string NamaBarangJasa { get; set; } = null!;
    public string? NamaSatuanUkur { get; set; }
    public decimal HargaSatuan { get; set; }
    public int JumlahBarangJasa { get; set; }
    public decimal TotalDiskon { get; set; }
    public decimal Dpp { get; set; }
    public decimal? DppNilaiLain { get; set; }
    public decimal TarifPpn { get; set; }
    public decimal Ppn { get; set; }
    public decimal TarifPpnbm { get; set; }
    public decimal Ppnbm { get; set; }
}