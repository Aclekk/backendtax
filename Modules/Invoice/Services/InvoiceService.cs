using TaxRefund.Api.Modules.Invoice.DTOs;
using TaxRefund.Api.Modules.Invoice.Repositories;
using TaxRefund.Api.Shared.Cegid;
using TaxRefund.Api.Shared.Database.Entities;
using TaxRefund.Api.Shared.Drs;
using TaxRefund.Api.Shared.Common;
using TaxRefund.Api.Modules.Negara.Repositories;

namespace TaxRefund.Api.Modules.Invoice.Services;

using System.Text.RegularExpressions;

public class InvoiceService(
    IInvoiceRepository repository,
    ICegidQueryRepository cegidRepository,
    IFakturXlsxGenerator xlsxGenerator,
    IMasterBarangFpRepository masterBarangFpRepository,
    IEmailService emailService,
    INegaraRepository negaraRepository,
    TaxRefund.Api.Modules.Traveler.Services.ITravelerService travelerService,
    ILogger<InvoiceService> logger) : IInvoiceService
{
    private const decimal MinimumTaxRefundAmount = 504545m;

    private readonly IInvoiceRepository _repository = repository;
    private readonly ICegidQueryRepository _cegidRepository = cegidRepository;
    private readonly IFakturXlsxGenerator _xlsxGenerator = xlsxGenerator;
    private readonly IMasterBarangFpRepository _masterBarangFpRepository = masterBarangFpRepository;
    private readonly IEmailService _emailService = emailService;   // ← BARU
    private readonly INegaraRepository _negaraRepository = negaraRepository;
    private readonly TaxRefund.Api.Modules.Traveler.Services.ITravelerService _travelerService = travelerService;
    private readonly ILogger<InvoiceService> _logger = logger;

    public async Task<InvoiceResponse> CreateInvoiceAsync(CreateInvoiceRequest request, string createdBy)
    {
        var exists = await _repository.NoInvoiceExistsAsync(request.NoInvoice, request.StoreCode);
        if (exists)
        {
            throw new InvalidOperationException($"Invoice dengan No. Invoice '{request.NoInvoice}' untuk toko '{request.StoreCode}' sudah ada.");
        }

        var cegidResults = await _cegidRepository.GetSalesTouristAsync(request.StoreCode, request.NoInvoice);
        if (cegidResults.Count == 0)
        {
            throw new InvalidOperationException(
                $"Data transaksi untuk No. Invoice '{request.NoInvoice}' (toko {request.StoreCode}) tidak ditemukan di CEGID.");
        }

        var transactionDate = DateOnly.FromDateTime(cegidResults[0].TransDate);
        var todayInWib = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
        if (transactionDate != todayInWib)
        {
            throw new InvalidOperationException(
                $"Invoice hanya dapat diajukan untuk transaksi tanggal {todayInWib:dd/MM/yyyy}. Tanggal transaksi: {transactionDate:dd/MM/yyyy}.");
        }

        var transactionTotal = cegidResults.Sum(result => ParseDecimalSafe(result.HargaTotal));
        if (transactionTotal < MinimumTaxRefundAmount)
        {
            throw new InvalidOperationException(
                $"Pengajuan Tax Refund minimal Rp{MinimumTaxRefundAmount:N0}. Total invoice ini Rp{transactionTotal:N0}.");
        }

        var countries = await _negaraRepository.GetAllAsync();
        var countryValue = request.NegaraPembeli.Trim();
        var countryCode = countries
            .FirstOrDefault(country =>
                string.Equals(country.Kode, countryValue, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(country.Nama, countryValue, StringComparison.OrdinalIgnoreCase))
            ?.Kode;

        if (string.IsNullOrWhiteSpace(countryCode))
        {
            throw new InvalidOperationException("Kode negara pembeli tidak valid.");
        }

        var entity = new TInvoice
        {
            NoInvoice = request.NoInvoice,
            StoreCode = request.StoreCode,
            NamaPembeli = string.IsNullOrWhiteSpace(request.NamaPembeli)
                ? "-"
                : request.NamaPembeli.Trim(),
            EmailPembeli = string.IsNullOrWhiteSpace(request.EmailPembeli)
                ? string.Empty
                : request.EmailPembeli.Trim(),
            NegaraPembeli = countryCode.ToUpperInvariant(),
            NomorDokumenPembeli = request.NomorDokumenPembeli,

            // --- Field paten, tidak dari FE ---
            TanggalFaktur = todayInWib,
            JenisFaktur = "Normal",
            KodeTransaksi = "06",
            KeteranganTambahan = string.Empty,
            DokumenPendukung = string.Empty,
            PeriodDokPendukung = string.Empty,
            CapFasilitas = string.Empty,
            IdTkuPenjual = "0749763322029000000000",
            NpwpNikPembeli = "0000000000000000",
            JenisIdPembeli = "Passport",
            IdTkuPembeli = new string('0', 22),

            // --- Diisi backend nanti pas download bila tidak ada data dari FE ---
            Referensi = string.Empty,
            AlamatPembeli = string.IsNullOrWhiteSpace(request.AlamatPembeli)
                ? string.Empty
                : request.AlamatPembeli.Trim(),

            Status = InvoiceStatus.New,
            CreatedBy = createdBy,
            // Store the SA upload timestamp as a WIB wall-clock value because
            // the existing database column does not carry timezone metadata.
            CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow.AddHours(7), DateTimeKind.Unspecified),
        };

        // Attempt to auto-link invoice to existing customer (traveler) by passport number.
        try
        {
            var passportKey = entity.NomorDokumenPembeli?.Trim();
            if (!string.IsNullOrWhiteSpace(passportKey))
            {
                var candidates = await _travelerService.GetTravelersAsync(passportKey);
                if (candidates?.Count > 0)
                {
                    var normalizedKey = Regex.Replace(passportKey.ToLowerInvariant(), "[^0-9a-z]", string.Empty);
                    var match = candidates.FirstOrDefault(c =>
                        Regex.Replace((c.PassportNumber ?? string.Empty).ToLowerInvariant(), "[^0-9a-z]", string.Empty)
                        == normalizedKey);
                    if (match is not null)
                    {
                        entity.CustomerId = match.Id;
                        _logger.LogInformation("Auto-linked invoice {Invoice} to customer id {CustomerId}", entity.NoInvoice, match.Id);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Auto-linking customer failed for invoice {Invoice}", entity.NoInvoice);
            // Don't fail invoice creation for auto-link errors.
        }

       var saved = await _repository.AddAsync(entity);
       var customerEmail = saved.EmailPembeli.Trim();

       var emailJobs = new List<(string Name, Func<Task> Send)>
       {
           (
               "customer",
               () => SendCustomerSubmissionEmailAsync(
                   customerEmail,
                   saved.NamaPembeli,
                   saved.NoInvoice,
                   saved.StoreCode,
                   transactionTotal)
           ),
           (
               "store",
               () => _emailService.SendStoreInvoiceSubmittedEmailAsync(
                   request.StoreEmail,
                   saved.NoInvoice,
                   saved.StoreCode,
                   saved.NamaPembeli,
                   transactionTotal)
           ),
           (
               "tax",
               () => _emailService.SendTaxInvoiceSubmittedEmailAsync(
                   saved.NoInvoice,
                   saved.StoreCode,
                   saved.NamaPembeli,
                   transactionTotal)
           ),
       };

       foreach (var emailJob in emailJobs)
       {
           try
           {
               await emailJob.Send();
               _logger.LogInformation(
                   "Email pengajuan {RecipientType} berhasil diproses untuk invoice {NoInvoice}",
                   emailJob.Name,
                   saved.NoInvoice);
           }
           catch (Exception ex)
           {
               _logger.LogError(
                   ex,
                   "Email pengajuan {RecipientType} gagal untuk invoice {NoInvoice}",
                   emailJob.Name,
                   saved.NoInvoice);
           }
       }

       // Reminder email is sent only by the scheduled background job at 18:00 WIB.
       // New invoice submission should not trigger an immediate reminder.
       return MapToResponse(saved);
    }

    private async Task SendCustomerSubmissionEmailAsync(
       string email,
       string namaPembeli,
       string noInvoice,
       string storeCode,
       decimal transactionTotal)
    {
       if (string.IsNullOrWhiteSpace(email))
       {
           _logger.LogWarning(
               "Email pengajuan customer dilewati karena EmailPembeli kosong untuk invoice {NoInvoice}",
               noInvoice);
           return;
       }

       Exception? lastException = null;
       for (var attempt = 1; attempt <= 2; attempt++)
       {
           try
           {
               await _emailService.SendInvoiceReceivedEmailAsync(
                   email,
                   namaPembeli,
                   noInvoice,
                   storeCode,
                   transactionTotal);
               return;
           }
           catch (Exception ex) when (attempt < 2)
           {
               lastException = ex;
               _logger.LogWarning(
                   ex,
                   "Percobaan pertama email pengajuan customer gagal untuk {Email}, mencoba ulang",
                   email);
               await Task.Delay(TimeSpan.FromSeconds(1));
           }
       }

       throw lastException ?? new InvalidOperationException(
           $"Email pengajuan customer gagal dikirim ke '{email}'.");
    }

    public async Task<PagedResponse<InvoiceResponse>> GetInvoicesAsync(int page, int pageSize)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(page, pageSize);

        return new PagedResponse<InvoiceResponse>
        {
            Items = items.Select(MapToResponse).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }

    public async Task<InvoiceResponse> DownloadInvoiceAsync(int id, string downloadedBy)
    {
        var invoice = await _repository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Invoice dengan id {id} tidak ditemukan.");

        if (invoice.Status != InvoiceStatus.New)
        {
            throw new InvalidOperationException(
                $"Invoice '{invoice.NoInvoice}' sudah pernah didownload atau bukan status baru.");
        }

        await EnrichFromCegidAsync(invoice);
        await _repository.MarkAsDownloadedAsync(invoice, downloadedBy);
        return MapToResponse(invoice);
    }

    public async Task<CegidPreviewResponse> PreviewCegidAsync(string storeCode, string noInvoice)
    {
        var cegidResults = await _cegidRepository.GetSalesTouristAsync(storeCode, noInvoice);
        if (cegidResults.Count == 0)
        {
            throw new KeyNotFoundException(
                $"Data transaksi untuk No. Invoice '{noInvoice}' (toko {storeCode}) tidak ditemukan di CEGID.");
        }

        var distinctStores = cegidResults
            .Select(x => x.StoreCode)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var selectedResults = cegidResults;

        if (string.IsNullOrWhiteSpace(storeCode) && distinctStores.Count > 1)
        {
            // Jangan gagal bila invoice ditemukan di beberapa site.
            // Biarkan FE menampilkan daftar storeCode dan user memilih store yang tepat.
            var fallbackStoreCode = distinctStores.First();
            selectedResults = cegidResults
                .Where(x => x.StoreCode.Equals(fallbackStoreCode, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
        else if (!string.IsNullOrWhiteSpace(storeCode))
        {
            selectedResults = cegidResults;
        }
        else if (distinctStores.Count > 0)
        {
            var fallbackStoreCode = distinctStores.First();
            selectedResults = cegidResults
                .Where(x => x.StoreCode.Equals(fallbackStoreCode, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (selectedResults.Count == 0)
        {
            selectedResults = cegidResults;
        }

        var header = selectedResults[0];
        var subtotal = selectedResults.Sum(r => ParseDecimalSafe(r.HargaTotal));
        var storeOptions = distinctStores
            .Select(storeCode =>
            {
                var storeRows = cegidResults
                    .Where(r => r.StoreCode.Equals(storeCode, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                return new CegidStoreOption
                {
                    StoreCode = storeCode,
                    StoreName = storeRows.FirstOrDefault()?.StoreName ?? string.Empty,
                    ItemCount = storeRows.Count,
                    Subtotal = storeRows.Sum(r => ParseDecimalSafe(r.HargaTotal)),
                };
            })
            .OrderBy(x => x.StoreCode)
            .ToList();

        var alreadyExists = false;
        try
        {
            // Check existence per store to allow same invoice number across different sites.
            alreadyExists = await _repository.NoInvoiceExistsAsync(noInvoice, header.StoreCode);
        }
        catch
        {
            alreadyExists = false;
        }

        return new CegidPreviewResponse
        {
            StoreCode = header.StoreCode,
            StoreName = header.StoreName,
            ReceiptNo = header.ReceiptNo,
            TransDate = header.TransDate,
            NamaMember = header.NamaMember,
            Email = header.Email,
            Address = header.Address,
            AvailableStoreCodes = distinctStores,
            StoreOptions = storeOptions,
            ItemCount = selectedResults.Count,
            Subtotal = subtotal,
            AlreadyExists = alreadyExists,
            Items = selectedResults.Select(r => new CegidPreviewItem
            {
                NamaBrg = r.NamaBrg,
                HargaSatuan = r.HargaSatuan,
                JumlahBrgJasa = r.JumlahBrgJasa,
                HargaTotal = r.HargaTotal,
                TotalDiskon = r.TotalDiskon,
                Dpp = r.Dpp,
                Ppn = r.Ppn,
            }).ToList(),
        };
    }

public async Task<(byte[] FileBytes, string FileName)> DownloadInvoiceXlsxAsync(int id, string downloadedBy)
{
    var timer = System.Diagnostics.Stopwatch.StartNew();
    var invoice = await _repository.GetByIdAsync(id)
        ?? throw new KeyNotFoundException($"Invoice dengan id {id} tidak ditemukan.");

    if (invoice.Status != InvoiceStatus.New)
    {
        throw new InvalidOperationException(
            $"Invoice '{invoice.NoInvoice}' sudah pernah didownload atau bukan status baru.");
    }

    var cegidItems = await _cegidRepository.GetSalesTouristAsync(invoice.StoreCode, invoice.NoInvoice);
    _logger.LogInformation(
        "Download invoice {InvoiceId}: CEGID selesai dalam {ElapsedMs} ms",
        invoice.Id,
        timer.ElapsedMilliseconds);
    if (cegidItems.Count == 0)
    {
        throw new InvalidOperationException(
            $"Data transaksi untuk invoice '{invoice.NoInvoice}' (toko {invoice.StoreCode}) tidak ditemukan di CEGID.");
    }

    var header = cegidItems[0];
    invoice.Referensi = invoice.NoInvoice;
    invoice.NamaPembeli = string.IsNullOrWhiteSpace(invoice.NamaPembeli)
        ? header.NamaMember
        : invoice.NamaPembeli;
    invoice.EmailPembeli = string.IsNullOrWhiteSpace(invoice.EmailPembeli)
        ? header.Email ?? string.Empty
        : invoice.EmailPembeli;
    invoice.AlamatPembeli = string.IsNullOrWhiteSpace(invoice.AlamatPembeli)
        ? header.Address ?? string.Empty
        : invoice.AlamatPembeli;

    var namaBarangKeys = cegidItems
        .Select(item => MasterBarangFpKey.FromBrandAndCategory(item.Brand, item.Category))
        .Where(key => key is not null)
        .Select(key => key!)
        .Distinct();

    Dictionary<string, string> kodeBarangMap;
    try
    {
        kodeBarangMap = await _masterBarangFpRepository.GetKodeBarangByNamaAsync(namaBarangKeys);
    }
    catch (Exception ex)
    {
        _logger.LogWarning(
            ex,
            "Lookup KodeBarangJasa ke DRS gagal untuk invoice {NoInvoice}, store {StoreCode}",
            invoice.NoInvoice,
            invoice.StoreCode);
        // Keep the download available while DRS is temporarily unreachable.
        // When DRS is available, the repository result is passed to the generator.
        kodeBarangMap = [];
    }
    _logger.LogInformation(
        "Download invoice {InvoiceId}: DRS selesai/fallback dalam {ElapsedMs} ms",
        invoice.Id,
        timer.ElapsedMilliseconds);

    var missingKeys = namaBarangKeys
        .Where(key => !kodeBarangMap.ContainsKey(key))
        .ToList();
    if (missingKeys.Count > 0)
    {
        _logger.LogWarning(
            "Sebanyak {MissingCount} mapping KodeBarangJasa tidak ditemukan di DRS untuk invoice {NoInvoice}: {MissingKeys}",
            missingKeys.Count,
            invoice.NoInvoice,
            string.Join(", ", missingKeys));
    }

    // Generate file DULU, sebelum status berubah — supaya kalau ini gagal,
    // status invoice tetap "New" dan bisa dicoba download ulang tanpa 409 palsu.
    var fileBytes = _xlsxGenerator.Generate(invoice, cegidItems, kodeBarangMap);
    _logger.LogInformation(
        "Download invoice {InvoiceId}: XLSX selesai dalam {ElapsedMs} ms",
        invoice.Id,
        timer.ElapsedMilliseconds);

    await _repository.MarkAsDownloadedAsync(invoice, downloadedBy);
    _logger.LogInformation(
        "Download invoice {InvoiceId}: status Downloaded tersimpan dalam {ElapsedMs} ms",
        invoice.Id,
        timer.ElapsedMilliseconds);

    return (fileBytes, BuildInvoiceFileName(invoice));
}

private static string BuildInvoiceFileName(TInvoice invoice)
{
    var segments = new[]
    {
        invoice.StoreCode,
        invoice.NoInvoice,
    };

    var sanitized = segments
        .Where(s => !string.IsNullOrWhiteSpace(s))
        .Select(s => new string(s.Where(char.IsLetterOrDigit).ToArray()))
        .Where(s => !string.IsNullOrWhiteSpace(s))
        .ToArray();

    var fileName = sanitized.Length > 0 ? string.Join("-", sanitized) : "invoice";
    return $"{fileName}.xlsx";
}

    private async Task EnrichFromCegidAsync(TInvoice invoice)
    {
        var cegidResults = await _cegidRepository.GetSalesTouristAsync(invoice.StoreCode, invoice.NoInvoice);
        if (cegidResults.Count == 0)
        {
            throw new InvalidOperationException(
                $"Data transaksi untuk invoice '{invoice.NoInvoice}' (toko {invoice.StoreCode}) tidak ditemukan di CEGID.");
        }

        var header = cegidResults[0];
        invoice.Referensi = invoice.NoInvoice;
        invoice.NamaPembeli = string.IsNullOrWhiteSpace(invoice.NamaPembeli)
            ? header.NamaMember
            : invoice.NamaPembeli;
        invoice.EmailPembeli = string.IsNullOrWhiteSpace(invoice.EmailPembeli)
            ? header.Email ?? string.Empty
            : invoice.EmailPembeli;
        invoice.AlamatPembeli = string.IsNullOrWhiteSpace(invoice.AlamatPembeli)
            ? header.Address ?? string.Empty
            : invoice.AlamatPembeli;
    }

    private static InvoiceResponse MapToResponse(TInvoice entity)
    {
        return new InvoiceResponse
        {
            Id = entity.Id,
            Baris = entity.Baris,
            NoInvoice = entity.NoInvoice,
            AlamatPembeli = entity.AlamatPembeli,
            StoreCode = entity.StoreCode,
            TanggalFaktur = entity.TanggalFaktur,
            JenisFaktur = entity.JenisFaktur,
            KodeTransaksi = entity.KodeTransaksi,
            KeteranganTambahan = entity.KeteranganTambahan,
            Referensi = entity.Referensi,
            NamaPembeli = entity.NamaPembeli,
            NegaraPembeli = entity.NegaraPembeli,
            NomorDokumenPembeli = entity.NomorDokumenPembeli,
            EmailPembeli = entity.EmailPembeli,
            Status = entity.Status.ToString(),
            CreatedBy = entity.CreatedBy,
            CreatedAt = entity.CreatedAt,
            CoreTaxFailureEmailSentAt = entity.CoreTaxFailureEmailSentAt,
        };
    }

    private static decimal ParseDecimalSafe(string value)
    {
        return decimal.TryParse(value, out var result) ? result : 0m;
    }
}
