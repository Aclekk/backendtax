using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using TaxRefund.Api.Shared.Database;
using TaxRefund.Api.Shared.Cegid;
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

static async Task EnsureCustomerTablesAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<TaxRefundDbContext>();
    var connection = dbContext.Database.GetDbConnection();

    await connection.OpenAsync();

    static async Task<bool> TableExistsAsync(DbConnection connection, string tableName)
    {
        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT EXISTS (
                SELECT 1
                FROM information_schema.tables
                WHERE table_schema = 'public'
                  AND table_name = @tableName
            );";

        var parameter = command.CreateParameter();
        parameter.ParameterName = "@tableName";
        parameter.Value = tableName;
        command.Parameters.Add(parameter);

        var result = await command.ExecuteScalarAsync();
        return result is bool exists && exists || result is long count && count == 1;
    }

    var customerTableExists = await TableExistsAsync(connection, "t_customer");
    if (!customerTableExists)
    {
        var legacyTableExists = await TableExistsAsync(connection, "t_traveler");
        if (legacyTableExists)
        {
            using var renameCommand = connection.CreateCommand();
            renameCommand.CommandText = "ALTER TABLE public.t_traveler RENAME TO t_customer;";
            await renameCommand.ExecuteNonQueryAsync();
            customerTableExists = true;
        }
    }

    var customerScanTableExists = await TableExistsAsync(connection, "t_customer_passport_scan");
    if (!customerScanTableExists)
    {
        var legacyScanTableExists = await TableExistsAsync(connection, "t_traveler_passport_scan");
        if (legacyScanTableExists)
        {
            using var renameCommand = connection.CreateCommand();
            renameCommand.CommandText = "ALTER TABLE public.t_traveler_passport_scan RENAME TO t_customer_passport_scan;";
            await renameCommand.ExecuteNonQueryAsync();
            customerScanTableExists = true;
        }
    }

    var activeCustomerTable = customerTableExists ? "t_customer" : "t_traveler";
    var columns = new List<string>();
    using (var command = connection.CreateCommand())
    {
        command.CommandText = @"
            SELECT column_name
            FROM information_schema.columns
            WHERE table_schema = 'public'
              AND table_name = @tableName
              AND column_name IN ('email', 'address');";

        var tableParameter = command.CreateParameter();
        tableParameter.ParameterName = "@tableName";
        tableParameter.Value = activeCustomerTable;
        command.Parameters.Add(tableParameter);

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            columns.Add(reader.GetString(0));
        }
    }

    var missingColumns = new List<string>();
    if (!columns.Contains("email")) missingColumns.Add("email character varying(255)");
    if (!columns.Contains("address")) missingColumns.Add("address character varying(500)");

    if (missingColumns.Count == 0)
    {
        return;
    }

    var alterSql = string.Join("; ", missingColumns.Select(col => $"ALTER TABLE public.{activeCustomerTable} ADD COLUMN IF NOT EXISTS {col};"));

    using var alterCommand = connection.CreateCommand();
    alterCommand.CommandText = alterSql;
    await alterCommand.ExecuteNonQueryAsync();
}

static async Task EnsureCoreTaxFailureEmailColumnAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<TaxRefundDbContext>();
    await dbContext.Database.ExecuteSqlRawAsync(
        "ALTER TABLE public.t_faktur_pajak ADD COLUMN IF NOT EXISTS coretax_failure_email_sent_at timestamp without time zone;");
    await dbContext.Database.ExecuteSqlRawAsync(
        "ALTER TABLE public.t_invoice ADD COLUMN IF NOT EXISTS coretax_failure_email_sent_at timestamp without time zone;");
}

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendDev", policy =>
    {
        policy.SetIsOriginAllowed(origin =>
              {
                  if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
                      return false;

                  return uri.Host is "localhost" or "127.0.0.1" or "110.239.86.241";
              })
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddScoped<TaxRefund.Api.Modules.Invoice.Repositories.IInvoiceRepository,
    TaxRefund.Api.Modules.Invoice.Repositories.InvoiceRepository>();
builder.Services.AddScoped<TaxRefund.Api.Modules.Invoice.Services.IInvoiceService,

    TaxRefund.Api.Modules.Invoice.Services.InvoiceService>();
builder.Services.AddScoped<TaxRefund.Api.Modules.Invoice.Services.IFakturXlsxGenerator,
    TaxRefund.Api.Modules.Invoice.Services.FakturXlsxGenerator>();
builder.Services.AddScoped<TaxRefund.Api.Modules.FakturPajak.Repositories.IFakturPajakRepository,
    TaxRefund.Api.Modules.FakturPajak.Repositories.FakturPajakRepository>();
builder.Services.AddScoped<TaxRefund.Api.Modules.FakturPajak.Services.IFakturPajakService,
    TaxRefund.Api.Modules.FakturPajak.Services.FakturPajakService>();
builder.Services.AddScoped<TaxRefund.Api.Modules.Negara.Services.INegaraService,
    TaxRefund.Api.Modules.Negara.Services.NegaraService>();
    builder.Services.AddScoped<
    TaxRefund.Api.Modules.Negara.Repositories.INegaraRepository,
    TaxRefund.Api.Modules.Negara.Repositories.NegaraRepository>();
 
builder.Services.AddScoped<
    TaxRefund.Api.Modules.Negara.Services.INegaraService,
    TaxRefund.Api.Modules.Negara.Services.NegaraService>();

builder.Services.AddScoped<TaxRefund.Api.Modules.Traveler.Services.ITravelerService,
    TaxRefund.Api.Modules.Traveler.Services.TravelerService>();
builder.Services.AddScoped<TaxRefund.Api.Shared.Common.IFileStorageService,
    TaxRefund.Api.Shared.Common.LocalFileStorageService>();
builder.Services.AddScoped<TaxRefund.Api.Shared.Common.IEmailService,
    TaxRefund.Api.Shared.Common.SmtpEmailService>();
builder.Services.AddHostedService<TaxRefund.Api.Shared.Common.PendingInvoiceReminderService>();   // ← TAMBAHKAN INI
builder.Services.AddMemoryCache();
builder.Services.AddScoped<TaxRefund.Api.Shared.Cegid.ICegidQueryRepository,
    TaxRefund.Api.Shared.Cegid.CegidQueryRepository>();
builder.Services.AddScoped<TaxRefund.Api.Shared.Drs.IMasterBarangFpRepository,
    TaxRefund.Api.Shared.Drs.MasterBarangFpRepository>();
builder.Services.AddDbContext<TaxRefundDbContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("TaxRefundDb"),
        npgsqlOptions => npgsqlOptions.MapEnum<TaxRefund.Api.Shared.Database.Entities.InvoiceStatus>("invoice_status")
                                       .MapEnum<TaxRefund.Api.Shared.Database.Entities.EmailLogStatus>("email_log_status")
    );
    options.LogTo(Console.WriteLine, Microsoft.Extensions.Logging.LogLevel.Information);
    options.EnableSensitiveDataLogging(); // biar keliatan parameter jelas juga
});

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();

var app = builder.Build();

await EnsureCustomerTablesAsync(app);
await EnsureCoreTaxFailureEmailColumnAsync(app);

if (!app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

app.UseCors("FrontendDev");

app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "tax-refund-api" }));
app.MapControllers();
app.MapOpenApi();
app.UseSwagger();
app.UseSwaggerUI();

app.Run();
