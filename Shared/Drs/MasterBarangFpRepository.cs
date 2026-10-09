using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace TaxRefund.Api.Shared.Drs;

public class MasterBarangFpRepository : IMasterBarangFpRepository
{
    private readonly string _connectionString;
    private readonly ILogger<MasterBarangFpRepository> _logger;

    public MasterBarangFpRepository(
        IConfiguration configuration,
        ILogger<MasterBarangFpRepository> logger)
    {
        _logger = logger;
        var configuredConnectionString = configuration.GetValue<string>("DrsDB:ConnectionString")
            ?? throw new InvalidOperationException("DrsDB:ConnectionString belum diset di appsettings.");

        const string passwordPlaceholder = "${DRS_DB_PASSWORD}";
        if (configuredConnectionString.Contains(passwordPlaceholder, StringComparison.Ordinal))
        {
            var password = Environment.GetEnvironmentVariable("DRS_DB_PASSWORD")
                ?? throw new InvalidOperationException(
                    "Environment variable DRS_DB_PASSWORD belum diset untuk connection string DRS yang memakai placeholder.");

            configuredConnectionString = configuredConnectionString.Replace(
                passwordPlaceholder,
                password,
                StringComparison.Ordinal);
        }

        // Microsoft.Data.SqlClient 7 default Encrypt=Mandatory.
        // Di Docker Linux (server Ubuntu) handshake TLS ke SQL Server DRS
        // sering gagal jika sertifikat tidak trusted.
        var builder = new SqlConnectionStringBuilder(configuredConnectionString)
        {
            TrustServerCertificate = true,
            Encrypt = SqlConnectionEncryptOption.Optional,
            ConnectRetryCount = 0,
            ConnectRetryInterval = 1,
        };
        // Lookup DRS hanya pelengkap file. Jangan menahan seluruh download terlalu lama
        // ketika server DRS sedang tidak dapat dijangkau.
        builder.ConnectTimeout = 5;

        _connectionString = builder.ConnectionString;
    }

    // ASUMSI (belum dikonfirmasi 100% ke Pak Chung/DBA DRS):
    // - Nama tabel: MasterBarangFP
    // - Kolom: Kode_Barang, Nama_Barang
    // Kalau nama tabel/kolom aslinya beda, tinggal ganti string SQL di bawah ini.
    public async Task<Dictionary<string, string>> GetKodeBarangByNamaAsync(IEnumerable<string> namaBarangList)
    {
        var namaList = namaBarangList
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(MasterBarangFpKey.Normalize)
            .Distinct()
            .ToList();

        if (namaList.Count == 0)
        {
            return new Dictionary<string, string>();
        }

        const string sql = @"
            SELECT
                LTRIM(RTRIM(Nama_Barang)) AS NamaBarang,
                LTRIM(RTRIM(Kode_Barang)) AS KodeBarang
            FROM MasterBarangFP
            WHERE UPPER(LTRIM(RTRIM(Nama_Barang))) IN @NamaList";

        await using var connection = new SqlConnection(_connectionString);

        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var rows = await connection.QueryAsync<MasterBarangFpRow>(new CommandDefinition(
                sql,
                new { NamaList = namaList },
                commandTimeout: 5,
                cancellationToken: timeout.Token));

            var map = rows
                .Where(r => !string.IsNullOrWhiteSpace(r.NamaBarang) && !string.IsNullOrWhiteSpace(r.KodeBarang))
                .GroupBy(r => MasterBarangFpKey.Normalize(r.NamaBarang))
                .ToDictionary(g => g.Key, g => g.First().KodeBarang.Trim());

            _logger.LogInformation(
                "Lookup MasterBarangFP sukses. Dicari={Requested}, Ketemu={Found}. Keys={Keys}",
                namaList.Count,
                map.Count,
                string.Join(", ", namaList));

            return map;
        }
        catch (Exception ex)
        {
            var cs = new SqlConnectionStringBuilder(_connectionString);
            _logger.LogError(
                ex,
                "Lookup MasterBarangFP gagal. Server={Server}, Database={Database}, Keys={Keys}",
                cs.DataSource,
                cs.InitialCatalog,
                string.Join(", ", namaList));
            throw;
        }
    }

    private sealed record MasterBarangFpRow(string NamaBarang, string KodeBarang);
}
