namespace TaxRefund.Api.Shared.Drs;

public interface IMasterBarangFpRepository
{
    /// <summary>
    /// Cari KodeBarangJasa (Kode_Barang) berdasarkan sekumpulan nama barang
    /// (format "{Brand}-{Category}"). Return dictionary supaya bisa lookup
    /// banyak item sekaligus dalam satu query (hindari N+1).
    /// Key = NamaBarang persis seperti yang dicari (uppercase),
    /// Value = Kode_Barang.
    /// </summary>
    Task<Dictionary<string, string>> GetKodeBarangByNamaAsync(IEnumerable<string> namaBarangList);
}