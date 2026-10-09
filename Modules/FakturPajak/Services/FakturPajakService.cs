using Microsoft.Extensions.Logging;
using TaxRefund.Api.Modules.FakturPajak.DTOs;
using TaxRefund.Api.Modules.FakturPajak.Repositories;
using TaxRefund.Api.Modules.Invoice.Repositories;
using TaxRefund.Api.Shared.Cegid;
using TaxRefund.Api.Shared.Cegid.Dtos;
using TaxRefund.Api.Shared.Common;
using TaxRefund.Api.Shared.Database.Entities;
using TaxRefund.Api.Shared.Drs;

namespace TaxRefund.Api.Modules.FakturPajak.Services;

public class FakturPajakService(
    IFakturPajakRepository fakturPajakRepository,
    IInvoiceRepository invoiceRepository,
    ICegidQueryRepository cegidRepository,
    IMasterBarangFpRepository masterBarangFpRepository,   // ← BARU
    IFileStorageService fileStorageService,
    IEmailService emailService,
    ILogger<FakturPajakService> logger) : IFakturPajakService
{
    private readonly IFakturPajakRepository _fakturPajakRepository = fakturPajakRepository;
    private readonly IInvoiceRepository _invoiceRepository = invoiceRepository;
    private readonly ICegidQueryRepository _cegidRepository = cegidRepository;
    private readonly IMasterBarangFpRepository _masterBarangFpRepository = masterBarangFpRepository;   // ← BARU
    private readonly IFileStorageService _fileStorageService = fileStorageService;
    private readonly IEmailService _emailService = emailService;
    private readonly ILogger<FakturPajakService> _logger = logger;

    public async Task<FakturPajakResponse> UploadAsync(int invoiceId, UploadFakturPajakRequest request, string uploadedBy)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(invoiceId)
            ?? throw new KeyNotFoundException($"Invoice dengan id {invoiceId} tidak ditemukan.");

        if (invoice.Status != InvoiceStatus.Downloaded)
        {
            throw new InvalidOperationException(
                $"Invoice '{invoice.NoInvoice}' belum didownload atau sudah diupload FP-nya.");
        }

        var desiredFileName = $"{invoice.StoreCode}{invoice.NoInvoice}";
        var (relativeFilePath, fileName) = await _fileStorageService.SaveAsync(request.File, desiredFileName);

        var fakturPajakEntity = new TFakturPajak
        {
            InvoiceId = invoice.Id,
            NomorFakturPajak = request.NomorFakturPajak?.Trim() ?? string.Empty,
            FilePath = relativeFilePath,
            FileName = fileName,
            UploadedBy = uploadedBy,
            UploadedAt = DateTime.Now,
            CreatedAt = DateTime.Now,
            CreatedBy = uploadedBy,
        };

        var details = await BuildDetailFakturFromCegidAsync(invoice.StoreCode, invoice.NoInvoice);

        var savedFakturPajak = await _fakturPajakRepository.AddAsync(fakturPajakEntity, details);

        await _fakturPajakRepository.UpdateInvoiceStatusAsync(invoice.Id, InvoiceStatus.FpUploaded);

        var finalStatus = await TrySendEmailAsync(savedFakturPajak.Id, invoice, relativeFilePath, fileName, sentBy: null);

        return await BuildResponseAsync(savedFakturPajak.Id, finalStatus);
    }

    public async Task<List<FakturPajakResponse>> GetAllAsync()
    {
        var entities = await _fakturPajakRepository.GetAllWithInvoiceAsync();
       return entities.Select(e => MapToResponse(e)).ToList();
    }

    public async Task<FakturPajakResponse> ResendEmailAsync(int fakturPajakId, string resentBy)
    {
        var fakturPajak = await _fakturPajakRepository.GetByIdAsync(fakturPajakId)
            ?? throw new KeyNotFoundException($"Faktur Pajak dengan id {fakturPajakId} tidak ditemukan.");

        var invoice = fakturPajak.Invoice
            ?? throw new InvalidOperationException("Data invoice terkait tidak ditemukan.");

        var finalStatus = await TrySendEmailAsync(
            fakturPajak.Id, invoice, fakturPajak.FilePath, fakturPajak.FileName, sentBy: resentBy);

        return await BuildResponseAsync(fakturPajak.Id, finalStatus);
    }

    public async Task SendCoreTaxFailureEmailAsync(int invoiceId, string sentBy)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(invoiceId)
            ?? throw new KeyNotFoundException($"Invoice dengan id {invoiceId} tidak ditemukan.");
        if (invoice.CoreTaxFailureEmailSentAt.HasValue)
        {
            throw new InvalidOperationException(
                $"Email kegagalan Coretax untuk invoice '{invoice.NoInvoice}' sudah pernah dikirim.");
        }

        if (string.IsNullOrWhiteSpace(invoice.EmailPembeli))
        {
            throw new InvalidOperationException(
                $"Email customer untuk invoice '{invoice.NoInvoice}' belum tersedia.");
        }

        await _emailService.SendCoreTaxFailureEmailAsync(
            invoice.EmailPembeli,
            invoice.NamaPembeli,
            invoice.NoInvoice);

        invoice.CoreTaxFailureEmailSentAt = DateTime.UtcNow;
        await _invoiceRepository.UpdateAsync(invoice);
    }

    private async Task<InvoiceStatus> TrySendEmailAsync(
        int fakturPajakId, TInvoice invoice, string relativeFilePath, string fileName, string? sentBy)
    {
        var absolutePath = _fileStorageService.GetAbsolutePath(relativeFilePath);

        try
            {
                await _emailService.SendFakturPajakEmailAsync(
                    invoice.EmailPembeli, invoice.NamaPembeli, invoice.NoInvoice, absolutePath, fileName);

                await _fakturPajakRepository.AddEmailLogAsync(new TEmailLog
{
    FakturPajakId = fakturPajakId,
    Status = EmailLogStatus.Success,
    CreatedBy = sentBy,                    // was SentBy
});

            await _fakturPajakRepository.SetEmailSentAtAsync(fakturPajakId, DateTime.UtcNow);
            await _fakturPajakRepository.UpdateInvoiceStatusAsync(invoice.Id, InvoiceStatus.Sent);

            return InvoiceStatus.Sent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gagal mengirim email Faktur Pajak untuk invoice {NoInvoice}", invoice.NoInvoice);

            await _fakturPajakRepository.AddEmailLogAsync(new TEmailLog
{
    FakturPajakId = fakturPajakId,
    Status = EmailLogStatus.Failed,
    ErrorMessage = ex.Message,
        CreatedBy = sentBy,
});

            await _fakturPajakRepository.UpdateInvoiceStatusAsync(invoice.Id, InvoiceStatus.EmailFailed);

            return InvoiceStatus.EmailFailed;
        }
    }

    private async Task<List<TFakturPajakDetail>> BuildDetailFakturFromCegidAsync(string storeCode, string noInvoice)
    {
        var cegidItems = await _cegidRepository.GetSalesTouristAsync(storeCode, noInvoice);
        if (cegidItems.Count == 0)
        {
            throw new InvalidOperationException(
                $"Data transaksi untuk invoice '{noInvoice}' (toko {storeCode}) tidak ditemukan di CEGID.");
        }

        // Key lookup KodeBarangJasa: format "{Brand}-{Category}" (uppercase),
        // sesuai pola MasterBarangFP (contoh: "COLORBOX-JACKET").
        // Kalau Brand atau Category kosong dari CEGID, key-nya dilewati —
        // KodeBarangJasa tetap kosong untuk item itu (bukan exception,
        // supaya 1 item bermasalah tidak menggagalkan seluruh upload).
        var kodeBarangKeys = cegidItems
            .Select(i => MasterBarangFpKey.FromBrandAndCategory(i.Brand, i.Category))
            .Where(k => k is not null)
            .Select(k => k!)
            .Distinct()
            .ToList();

        Dictionary<string, string> kodeBarangMap = [];
        try
        {
            kodeBarangMap = kodeBarangKeys.Count > 0
                ? await _masterBarangFpRepository.GetKodeBarangByNamaAsync(kodeBarangKeys)
                : [];
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lookup MasterBarangFP gagal untuk invoice {NoInvoice}; proses dilanjutkan tanpa mapping DRS", noInvoice);
            kodeBarangMap = [];
        }

        var details = new List<TFakturPajakDetail>();
        var isBatamStore = StoreTaxRules.IsBatamStore(storeCode, null, cegidItems.FirstOrDefault()?.StoreCity, cegidItems.FirstOrDefault()?.Provinsi);

        foreach (var (item, index) in cegidItems.Select((item, index) => (item, index)))
        {
            var key = MasterBarangFpKey.FromBrandAndCategory(item.Brand, item.Category);
            var kodeBarangJasa = string.Empty;
            var ppnValue = isBatamStore ? 0m : ParseDecimalSafe(item.Ppn);
            var tarifPpnValue = isBatamStore ? 0m : ParseDecimalSafe(item.Pct);

            if (key is not null)
            {
                if (kodeBarangMap.TryGetValue(key, out var kode))
                {
                    kodeBarangJasa = kode;
                }
                else
                {
                    // Tidak ketemu di MasterBarangFP — log supaya kelihatan
                    // di data mana yang belum ke-cover, tapi tidak menggagalkan proses.
                    _logger.LogWarning(
                        "KodeBarangJasa tidak ditemukan di MasterBarangFP untuk key '{Key}' (invoice {NoInvoice})",
                        key, noInvoice);
                }
            }

            details.Add(new TFakturPajakDetail
            {
                Baris = index + 1,
                BarangJasa = "A",
                KodeBarangJasa = Truncate(kodeBarangJasa, 20),
                NamaBarangJasa = Truncate(item.NamaBrg, 255),
                NamaSatuanUkur = "UM.0021",
                CreatedAt = DateTime.Now,
                HargaSatuan = ParseDecimalSafe(item.HargaSatuan),
                JumlahBarangJasa = (int)ParseDecimalSafe(item.JumlahBrgJasa),
                TotalDiskon = ParseDecimalSafe(item.TotalDiskon),
                Dpp = ParseDecimalSafe(item.Dpp),
                DppNilaiLain = ParseDecimalSafe(item.DppNilaiLain),
                TarifPpn = tarifPpnValue,
                Ppn = ppnValue,
                TarifPpnbm = 0,
                Ppnbm = 0,
            });
        }

        return details;
    }

    private static decimal ParseDecimalSafe(string? value)
    {
        return decimal.TryParse(value, out var result) ? result : 0m;
    }

    private static string Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
        {
            return value ?? string.Empty;
        }

        return value[..maxLength];
    }

    private async Task<FakturPajakResponse> BuildResponseAsync(int fakturPajakId, InvoiceStatus latestStatus)
    {
        var fp = await _fakturPajakRepository.GetByIdAsync(fakturPajakId)
            ?? throw new KeyNotFoundException("Faktur Pajak tidak ditemukan setelah diproses.");

        return MapToResponse(fp, latestStatus);
    }

    private static FakturPajakResponse MapToResponse(TFakturPajak entity, InvoiceStatus? overrideStatus = null)
    {
        return new FakturPajakResponse
        {
            Id = entity.Id,
            InvoiceId = entity.InvoiceId,
            NoInvoice = entity.Invoice?.NoInvoice ?? "",
            NamaPembeli = entity.Invoice?.NamaPembeli ?? "",
            NegaraPembeli = entity.Invoice?.NegaraPembeli ?? "",
            NomorFakturPajak = entity.NomorFakturPajak,
            FileName = entity.FileName,
            UploadedBy = entity.UploadedBy,
            UploadedAt = entity.UploadedAt,
            EmailSentAt = entity.EmailSentAt,
            InvoiceStatus = (overrideStatus ?? entity.Invoice?.Status ?? InvoiceStatus.FpUploaded).ToString(),
        };
    }
}