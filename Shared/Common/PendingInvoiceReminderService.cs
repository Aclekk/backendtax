using TaxRefund.Api.Modules.Invoice.Repositories;
using TaxRefund.Api.Shared.Common;

namespace TaxRefund.Api.Shared.Common;

public class PendingInvoiceReminderService(
    IServiceProvider serviceProvider,
    ILogger<PendingInvoiceReminderService> logger) : BackgroundService
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly ILogger<PendingInvoiceReminderService> _logger = logger;

    private static readonly TimeZoneInfo JakartaTimeZone = GetJakartaTimeZone();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var nowUtc = DateTime.UtcNow;
            var nowJakarta = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, JakartaTimeZone);
            var nextJakartaRun = new DateTime(nowJakarta.Year, nowJakarta.Month, nowJakarta.Day, 18, 0, 0, DateTimeKind.Unspecified);
            if (nowJakarta >= nextJakartaRun)
            {
                nextJakartaRun = nextJakartaRun.AddDays(1);
            }

            var nextUtcRun = TimeZoneInfo.ConvertTimeToUtc(nextJakartaRun, JakartaTimeZone);
            var delay = nextUtcRun - nowUtc;

            _logger.LogInformation(
                "PendingInvoiceReminderService: next run at {NextRunWib} (WIB)",
                TimeZoneInfo.ConvertTimeFromUtc(nextUtcRun, JakartaTimeZone).ToString("yyyy-MM-dd HH:mm:ss"));

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var invoiceRepository = scope.ServiceProvider.GetRequiredService<IInvoiceRepository>();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                var pendingCount = await invoiceRepository.CountPendingAsync();

                if (pendingCount > 0)
                {
                    await emailService.SendPendingInvoiceReminderAsync(pendingCount);
                    _logger.LogInformation("Reminder terkirim, pendingCount = {Count}", pendingCount);
                }
                else
                {
                    _logger.LogInformation("Tidak ada invoice pending, reminder tidak dikirim.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gagal menjalankan PendingInvoiceReminderService");
            }
        }
    }

    private static TimeZoneInfo GetJakartaTimeZone()
    {
        var candidates = new[]
        {
            "Asia/Jakarta",
            "SE Asia Standard Time",
            "Asia/Bangkok",
            "UTC"
        };

        foreach (var candidate in candidates)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(candidate);
            }
            catch (TimeZoneNotFoundException)
            {
                // continue to next candidate
            }
            catch (InvalidTimeZoneException)
            {
                // continue to next candidate
            }
        }

        return TimeZoneInfo.Local;
    }
}