using System.Collections.Concurrent;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using TaxRefund.Api.Shared.Cegid.Dtos;

namespace TaxRefund.Api.Shared.Cegid;

public class CegidQueryRepository : ICegidQueryRepository
{
    private readonly string _connectionString;
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _cache;
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> CacheLocks = new();

    public CegidQueryRepository(IConfiguration configuration, IMemoryCache cache)
    {
        _configuration = configuration;
        _cache = cache;
        var configuredConnectionString = configuration.GetValue<string>("CegidDB:ConnectionString") ?? string.Empty;
        if (string.IsNullOrWhiteSpace(configuredConnectionString))
        {
            _connectionString = string.Empty;
        }
        else
        {
            var builder = new SqlConnectionStringBuilder(configuredConnectionString)
            {
                ConnectTimeout = 10,
                ConnectRetryCount = 0,
                ConnectRetryInterval = 1,
            };
            _connectionString = builder.ConnectionString;
        }
        // Do not throw here: allow running in dev without CEGID configured and fall back to mock data when appropriate.
    }

    public async Task<List<CegidSalesTouristResult>> GetSalesTouristAsync(string storeCode, string noInvoice)
    {
        var cacheKey = $"cegid-sales-tourist:{storeCode.Trim().ToUpperInvariant()}:{noInvoice.Trim().ToUpperInvariant()}";
        if (_cache.TryGetValue(cacheKey, out List<CegidSalesTouristResult>? cached) && cached is not null)
        {
            return cached;
        }

        var cacheLock = CacheLocks.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));
        await cacheLock.WaitAsync();
        try
        {
            if (_cache.TryGetValue(cacheKey, out cached) && cached is not null)
            {
                return cached;
            }

            var result = await QuerySalesTouristAsync(storeCode, noInvoice);
            var isMockResult = result.Count > 0 &&
                string.Equals(result[0].StoreName, "Sample Store", StringComparison.OrdinalIgnoreCase);
            if (result.Count > 0)
            {
                _cache.Set(
                    cacheKey,
                    result,
                    isMockResult ? TimeSpan.FromMinutes(5) : TimeSpan.FromHours(2));
            }

            return result;
        }
        finally
        {
            cacheLock.Release();
        }
    }

    private async Task<List<CegidSalesTouristResult>> QuerySalesTouristAsync(string storeCode, string noInvoice)
    {
        // Hanya SELECT ke CEGID. Tidak ada INSERT / UPDATE / DELETE.
        // Query ini juga sudah ditambah store_city, provinsi, Brand, Gender,
        // category (Pak Chungyie) — Brand + Category dipakai FakturPajakService
        // untuk lookup KodeBarangJasa ke MasterBarangFP (DRS), format "{Brand}-{Category}".
        const string sql = @"
select
    GL_SOUCHE as StoreCode,
    ET_LIBELLE as StoreName,
    ET_VILLE as StoreCity,
    RG_LIBELLE as Provinsi,
    GL_NUMERO as ReceiptNo,
    cast(GL_DATEPIECE as date) as TransDate,
    GL_TIERS as NomorMember,
    case 
        when LTRIM(RTRIM(T_PRENOM)) = LTRIM(RTRIM(T_LIBELLE))
            then LTRIM(RTRIM(T_LIBELLE))
        when T_LIBELLE <> '' and T_PRENOM = '' 
            then LTRIM(RTRIM(T_LIBELLE))
        when T_LIBELLE = '' and T_PRENOM <> ''
            then LTRIM(RTRIM(T_PRENOM))
        else LTRIM(RTRIM(T_PRENOM)) + ' ' + LTRIM(RTRIM(T_LIBELLE)) 
    end as NamaMember,
    T_Email as Email,
    COALESCE(
        NULLIF(CONCAT_WS(' ',
            NULLIF(LTRIM(RTRIM(T_ADRESSE1)), ''),
            NULLIF(LTRIM(RTRIM(T_ADRESSE2)), ''),
            NULLIF(LTRIM(RTRIM(T_ADRESSE3)), ''),
            NULLIF(LTRIM(RTRIM(T_CODEPOSTAL)), ''),
            NULLIF(LTRIM(RTRIM(T_VILLE)), '')
        ), ''),
        NULLIF(CONCAT_WS(' ',
            NULLIF(LTRIM(RTRIM(ALAMAT1.MUD_VALCHAMP)), ''),
            NULLIF(LTRIM(RTRIM(ALAMAT2.MUD_VALCHAMP)), ''),
            NULLIF(LTRIM(RTRIM(ALAMAT3.MUD_VALCHAMP)), '')
        ), '')
    ) as Address,
    concat_WS(' ', nullif(TRIM(g.GA_LIBELLE),''), nullif(LIBDIM1,''), nullif(LIBDIM2,'')) as NamaBrg,
    BRAND.CC_LIBELLE as Brand,
    GENDER.CC_LIBELLE as Gender,
    PRODUCTCATEGORY.CC_LIBELLE as Category,
    cast(cast(round(cast(GL_PUHT as decimal(18,2)),0,0) as decimal(18,0)) as varchar(50)) as HargaSatuan,
    cast(cast(sum(GL_QTEFACT) as INT) as varchar(50)) as JumlahBrgJasa,
    cast(cast(round(cast(sum(GL_MONTANTHT) as decimal(18,2)),0,0) as decimal(18,0)) as varchar(50)) HargaTotal,
    cast(
        cast(round(cast(sum(GL_MONTANTHT) as decimal(18,2)),0,0) as decimal(18,0)) -
        cast(round(cast(sum(GL_TOTALHT) as decimal(18,2)),0,0) as decimal(18,0))
        as varchar(50)) as TotalDiskon,
    cast(cast(round(cast(sum(GL_TOTALHT) as decimal(18,2)),0,0) as decimal(18,0)) as varchar(50)) as Dpp,
    cast(cast(round(cast(sum(GL_TOTALHT*11/12) as decimal(18,2)),0,0) as decimal(18,0)) as varchar(50)) as DppNilaiLain,
    cast(cast(round(cast(100*sum(GL_TOTALTAXE1)/NULLIF(sum(GL_TOTALHT*11/12),0) as decimal(18,2)),0,0) as decimal(18,0)) as varchar(50)) as Pct,
    cast(cast(round(cast(sum(GL_TOTALTAXE1) as decimal(18,2)),0,0) as decimal(18,0)) as varchar(50)) as Ppn
from GCLIGNEARTDIM g
    JOIN ETABLISS ET1 ON GL_ETABLISSEMENT = ET1.ET_ETABLISSEMENT
    JOIN ARTICLE_COMPL ON GL_ARTICLE = ARTICLE_COMPL.GA_ARTICLE
    LEFT JOIN MCHPSUTILISATDATA UDF1
        ON UDF1.MUD_ENTITEMCD = 'CLI' AND UDF1.MUD_CLEENTITE = T_TIERS AND UDF1.MUD_IDCHAMP = 50
    LEFT JOIN MCHPSUTILISATDATA ALAMAT1
        ON ALAMAT1.MUD_ENTITEMCD = 'CLI' AND ALAMAT1.MUD_CLEENTITE = T_TIERS AND ALAMAT1.MUD_IDCHAMP = 31
    LEFT JOIN MCHPSUTILISATDATA ALAMAT2
        ON ALAMAT2.MUD_ENTITEMCD = 'CLI' AND ALAMAT2.MUD_CLEENTITE = T_TIERS AND ALAMAT2.MUD_IDCHAMP = 32
    LEFT JOIN MCHPSUTILISATDATA ALAMAT3
        ON ALAMAT3.MUD_ENTITEMCD = 'CLI' AND ALAMAT3.MUD_CLEENTITE = T_TIERS AND ALAMAT3.MUD_IDCHAMP = 33
    LEFT JOIN CHOIXCOD BRAND
        ON BRAND.CC_TYPE = 'GDM' AND BRAND.CC_CODE = ARTICLE_COMPL.GA_DOMAINE
    LEFT JOIN CHOIXCOD GENDER
        ON GENDER.CC_TYPE = 'FN1' AND GENDER.CC_CODE = ARTICLE_COMPL.GA_FAMILLENIV1
    LEFT JOIN CHOIXCOD PRODUCTCATEGORY
        ON PRODUCTCATEGORY.CC_TYPE = 'FN4' AND PRODUCTCATEGORY.CC_CODE = ARTICLE_COMPL.GA2_FAMILLENIV4
    LEFT JOIN REGION WITH (NOLOCK) ON RG_PAYS = 'IDN' AND RG_REGION = ET_REGION
where GL_NUMERO = @NoInvoice
and (@StoreCode is null or GL_SOUCHE = @StoreCode)
    and GL_NATUREPIECEG = 'FFO'
    and ET_TYPEETAB in ('SUC')
    and GP_TYPEPROVENANCE <> 'ECO'
    and gp_ticketannule <> 'X'
    and gp_typecompta in ('TIC')
    and gp_supprime <> 'X'
    and ARTICLE_COMPL.GA_TYPEARTICLE not in ('FI')
group by
    GL_SOUCHE, ET_LIBELLE, ET_VILLE, RG_LIBELLE, GL_NUMERO, GL_DATEPIECE, GL_TIERS, T_PRENOM, T_LIBELLE,
    T_ADRESSE1, T_ADRESSE2, T_ADRESSE3, T_CODEPOSTAL, T_VILLE, T_Email,
    ALAMAT1.MUD_VALCHAMP, ALAMAT2.MUD_VALCHAMP, ALAMAT3.MUD_VALCHAMP,
    GL_REFARTBARRE, g.GA_LIBELLE, LIBDIM1, LIBDIM2,
    BRAND.CC_LIBELLE, GENDER.CC_LIBELLE, PRODUCTCATEGORY.CC_LIBELLE,
    GL_PUHT";
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(timeout.Token);
            await using (var isolation = new SqlCommand("SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;", connection))
            {
                await isolation.ExecuteNonQueryAsync(timeout.Token);
            }

            var result = await connection.QueryAsync<CegidSalesTouristResult>(new CommandDefinition(
                sql,
                new { NoInvoice = noInvoice, StoreCode = string.IsNullOrWhiteSpace(storeCode) ? null : storeCode },
                commandTimeout: 10,
                cancellationToken: timeout.Token));

            return result.AsList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CEGID QUERY ERROR] {ex.Message}");

            // Jika environment development, kembalikan sample/mock data agar fitur preview dapat diuji secara lokal.
            var env = _configuration["ASPNETCORE_ENVIRONMENT"] ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
            if (env.Equals("Development", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(_connectionString))
            {
                var sample = new CegidSalesTouristResult
                {
                    StoreCode = string.IsNullOrWhiteSpace(storeCode) ? "SAMPLE" : storeCode,
                    StoreName = "Sample Store",
                    StoreCity = "Jakarta",
                    Provinsi = "DKI Jakarta",
                    ReceiptNo = noInvoice,
                    TransDate = DateTime.UtcNow.Date,
                    NomorMember = "",
                    NamaMember = "Sample Buyer",
                    Email = "sample.buyer@example.com",
                    Address = "Jl. Contoh No.1, Jakarta",
                    NamaBrg = "Sample Product",
                    Brand = "SampleBrand",
                    Category = "SampleCategory",
                    HargaSatuan = "100000",
                    JumlahBrgJasa = "1",
                    HargaTotal = "100000",
                    TotalDiskon = "0",
                    Dpp = "90000",
                    DppNilaiLain = "0",
                    Pct = "10",
                    Ppn = "10000",
                };

                return new List<CegidSalesTouristResult> { sample };
            }

            // Otherwise, return empty list to indicate not found without crashing the API.
            return new List<CegidSalesTouristResult>();
        }
    }
}
