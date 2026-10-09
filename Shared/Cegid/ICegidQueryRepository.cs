using TaxRefund.Api.Shared.Cegid.Dtos;

namespace TaxRefund.Api.Shared.Cegid;

public interface ICegidQueryRepository
{
    /// <summary>
    /// Ambil data penjualan turis dari CEGID berdasarkan store code + nomor invoice.
    /// Return list kosong kalau invoice tidak ditemukan.
    /// </summary>
    Task<List<CegidSalesTouristResult>> GetSalesTouristAsync(string storeCode, string noInvoice);
}