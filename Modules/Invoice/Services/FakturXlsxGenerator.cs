using ClosedXML.Excel;
using TaxRefund.Api.Shared.Cegid.Dtos;
using TaxRefund.Api.Shared.Common;
using TaxRefund.Api.Shared.Database.Entities;
using TaxRefund.Api.Shared.Drs;

namespace TaxRefund.Api.Modules.Invoice.Services;

public interface IFakturXlsxGenerator
{
    byte[] Generate(
        TInvoice invoice,
        List<CegidSalesTouristResult> cegidItems,
        Dictionary<string, string> kodeBarangMap);
}

public class FakturXlsxGenerator : IFakturXlsxGenerator
{
    // Baris pertama data (setelah header) di masing-masing sheet template.
    private const int FakturDataRow = 4;
    private const int DetailFakturDataRow = 2;

public byte[] Generate(
    TInvoice invoice,
    List<CegidSalesTouristResult> cegidItems,
    Dictionary<string, string> kodeBarangMap)
{
    using var templateStream = GetTemplateStream();
    using var workbook = new XLWorkbook(templateStream);

    FillFakturSheet(GetWorksheet(workbook, "Faktur"), invoice);
    FillDetailFakturSheet(GetWorksheet(workbook, "DetailFaktur"), invoice, cegidItems, kodeBarangMap);

    using var output = new MemoryStream();
    workbook.SaveAs(output);
    return output.ToArray();
}
private static IXLWorksheet GetWorksheet(XLWorkbook workbook, string name)
{
    if (!workbook.Worksheets.TryGetWorksheet(name, out var sheet))
    {
        var available = string.Join(", ", workbook.Worksheets.Select(ws => ws.Name));
        throw new InvalidOperationException(
            $"Sheet '{name}' tidak ditemukan di template. Sheet yang tersedia: {available}");
    }
    return sheet;
}

 private static Stream GetTemplateStream()
{
    var assembly = typeof(FakturXlsxGenerator).Assembly;
    const string resourceName = "TaxRefund.Api.Shared.Templates.faktur-template.xlsx";

    var stream = assembly.GetManifestResourceStream(resourceName);
    if (stream is null)
    {
        var available = string.Join(", ", assembly.GetManifestResourceNames());
        throw new InvalidOperationException(
            $"Template xlsx '{resourceName}' tidak ditemukan sebagai embedded resource. " +
            $"Resource yang tersedia: {available}");
    }
    return stream;
}

    private static void FillFakturSheet(IXLWorksheet sheet, TInvoice invoice)
    {
        var row = sheet.Row(FakturDataRow);

        row.Cell(1).Value = invoice.Baris ?? 1;                                  // Baris
        // Store the value as an Excel date while keeping the required display format.
        row.Cell(2).Value = invoice.TanggalFaktur.ToDateTime(TimeOnly.MinValue);
        // Built-in format 14 keeps the cell categorized as Date and follows
        // the workbook/Excel locale for day-month-year display.
        row.Cell(2).Style.DateFormat.NumberFormatId = 14;
        row.Cell(3).Value = invoice.JenisFaktur;                                 // Jenis Faktur
        row.Cell(4).Value = invoice.KodeTransaksi;                               // Kode Transaksi
        row.Cell(5).Value = invoice.KeteranganTambahan ?? string.Empty;          // Keterangan Tambahan
        row.Cell(6).Value = invoice.DokumenPendukung ?? string.Empty;            // Dokumen Pendukung
        row.Cell(7).Value = invoice.PeriodDokPendukung ?? string.Empty;          // Period Dok Pendukung
        row.Cell(8).Value = invoice.Referensi ?? string.Empty;                   // Referensi
        row.Cell(9).Value = invoice.CapFasilitas ?? string.Empty;                // Cap Fasilitas
        row.Cell(10).Value = invoice.IdTkuPenjual;                               // ID TKU Penjual
        row.Cell(11).Value = invoice.NpwpNikPembeli;                             // NPWP/NIK Pembeli
        row.Cell(12).Value = invoice.JenisIdPembeli;                             // Jenis ID Pembeli
        row.Cell(13).Value = invoice.NegaraPembeli;                              // Negara Pembeli
        row.Cell(14).Value = invoice.NomorDokumenPembeli;                        // Nomor Dokumen Pembeli
        row.Cell(15).Value = invoice.NamaPembeli;                                // Nama Pembeli
        row.Cell(16).Value = invoice.AlamatPembeli;                              // Alamat Pembeli
        row.Cell(17).Value = invoice.EmailPembeli;                               // Email Pembeli
        row.Cell(18).Value = string.IsNullOrWhiteSpace(invoice.IdTkuPembeli)
            ? new string('0', 22)
            : invoice.IdTkuPembeli;                                             // ID TKU Pembeli
    }

  private static void FillDetailFakturSheet(
    IXLWorksheet sheet,
    TInvoice invoice,
    List<CegidSalesTouristResult> cegidItems,
    Dictionary<string, string> kodeBarangMap)
{
    var currentRow = DetailFakturDataRow;
    var lastTemplateRow = sheet.LastRowUsed()?.RowNumber() ?? currentRow;
    if (lastTemplateRow >= currentRow)
    {
        sheet.Range(currentRow, 1, lastTemplateRow, 14).Clear(XLClearOptions.Contents);
    }

    var isBatamStore = StoreTaxRules.IsBatamStore(
        invoice.StoreCode,
        cegidItems.FirstOrDefault()?.StoreName,
        cegidItems.FirstOrDefault()?.StoreCity,
        cegidItems.FirstOrDefault()?.Provinsi);

    foreach (var item in cegidItems)
    {
        var row = sheet.Row(currentRow);
        var key = MasterBarangFpKey.FromBrandAndCategory(item.Brand, item.Category);
        var ppnValue = isBatamStore ? 0m : ParseDecimalSafe(item.Ppn);
        var tarifPpnValue = isBatamStore ? 0m : ParseDecimalSafe(item.Pct);

        row.Cell(1).Value = invoice.Baris ?? 1;
        row.Cell(2).Value = "A";
        row.Cell(3).Value = key is not null && kodeBarangMap.TryGetValue(key, out var kode)
            ? kode
            : string.Empty;
        row.Cell(4).Value = item.NamaBrg;
        row.Cell(5).Value = "UM.0021";
        row.Cell(6).Value = ParseDecimalSafe(item.HargaSatuan);
        row.Cell(7).Value = ParseDecimalSafe(item.JumlahBrgJasa);
        row.Cell(8).Value = ParseDecimalSafe(item.TotalDiskon);
        row.Cell(9).Value = ParseDecimalSafe(item.Dpp);
        row.Cell(10).Value = ParseDecimalSafe(item.DppNilaiLain);
        row.Cell(11).Value = tarifPpnValue;
        row.Cell(12).Value = ppnValue;
        row.Cell(13).Value = 0;
        row.Cell(14).Value = 0;

        currentRow++;
    }

    sheet.Row(currentRow).Cell(1).Value = "END";
}

private static decimal ParseDecimalSafe(string? value)
{
    return decimal.TryParse(value, out var result) ? result : 0m;
}
}