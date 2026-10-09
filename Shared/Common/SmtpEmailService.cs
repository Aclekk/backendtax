using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace TaxRefund.Api.Shared.Common;

public class SmtpEmailService(IConfiguration configuration) : IEmailService
{
    private readonly IConfiguration _configuration = configuration;
    private const int SmtpTimeoutMilliseconds = 15_000;
    // Override recipient for store and tax team notifications (forced). Change here to redirect those emails.
    private const string OverrideRecipientEmail = "ziyadzechran@gmail.com";
    private const string StoreFallbackEmail = "taxrefund@delamibrands.com";
    private const string TaxTeamEmail = OverrideRecipientEmail;

    private string ResolveStoreRecipient(string? explicitEmail)
    {
        // If an override recipient is configured, always use it for store notifications.
        if (!string.IsNullOrWhiteSpace(OverrideRecipientEmail))
        {
            return OverrideRecipientEmail;
        }

        if (!string.IsNullOrWhiteSpace(explicitEmail))
        {
            return explicitEmail.Trim();
        }

        return StoreFallbackEmail;
    }

    public async Task SendFakturPajakEmailAsync(
        string toEmail,
        string namaPembeli,
        string noInvoice,
        string attachmentAbsolutePath,
        string attachmentFileName)
    {
        var destinationEmail = string.IsNullOrWhiteSpace(toEmail)
            ? _configuration["Smtp:FromEmail"] ?? "noreply@example.com"
            : toEmail;

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(
            _configuration["Smtp:FromName"],
            _configuration["Smtp:FromEmail"]));
        message.To.Add(new MailboxAddress(namaPembeli, destinationEmail));
        message.Subject = $"Tax Invoice - Invoice {noInvoice}";

        var builder = new BodyBuilder
        {
            HtmlBody = $"""
                <p>Hello {namaPembeli},</p>
                <p>Please find attached the Tax Invoice for your transaction with Delami Brands.</p>
                <p><strong>Invoice Number:</strong> {noInvoice}</p>
                <p>Please keep this document for your records.</p>
                <p>Thank you for shopping with Delami Brands.</p>
                <p>Best regards,<br/>DelamiBrands</p>
                """,
        };

        await builder.Attachments.AddAsync(attachmentAbsolutePath);
        message.Body = builder.ToMessageBody();

        using var client = new SmtpClient();
        client.Timeout = SmtpTimeoutMilliseconds;
        await client.ConnectAsync(
            _configuration["Smtp:Host"],
            int.Parse(_configuration["Smtp:Port"] ?? "587"),
            SecureSocketOptions.StartTls);

        await client.AuthenticateAsync(
            _configuration["Smtp:User"],
            _configuration["Smtp:Pass"]);

        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }

    public async Task SendCoreTaxFailureEmailAsync(
        string toEmail,
        string namaPembeli,
        string noInvoice)
    {
        var destinationEmail = string.IsNullOrWhiteSpace(toEmail)
            ? _configuration["Smtp:FromEmail"] ?? "noreply@example.com"
            : toEmail;

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(
            _configuration["Smtp:FromName"],
            _configuration["Smtp:FromEmail"]));
        message.To.Add(new MailboxAddress(namaPembeli, destinationEmail));
        message.Subject = $"Update Tax Invoice Submission - {noInvoice}";

        var builder = new BodyBuilder
        {
            HtmlBody = $"""
                <p>Dear Customer,</p>
                <p>With reference to Invoice No. {noInvoice}, we regret to inform you that we are unable to issue a Tax Invoice for this transaction.</p>
                <p>Should you have any questions or require further information, please feel free to contact us at taxrefund@delamibrands.com</p>
                <p>Thank you for your kind understanding.</p>
                <p>Best regards,<br/>PT Delamibrands Kharisma Busana</p>
                """,
        };
        message.Body = builder.ToMessageBody();

        using var client = new SmtpClient();
        client.Timeout = SmtpTimeoutMilliseconds;
        await client.ConnectAsync(
            _configuration["Smtp:Host"],
            int.Parse(_configuration["Smtp:Port"] ?? "587"),
            SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(
            _configuration["Smtp:User"],
            _configuration["Smtp:Pass"]);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }

    public async Task SendInvoiceReceivedEmailAsync(
        string toEmail,
        string namaPembeli,
        string noInvoice,
        string storeCode,
        decimal transactionValue)
    {
        var destinationEmail = string.IsNullOrWhiteSpace(toEmail)
            ? _configuration["Smtp:FromEmail"] ?? "noreply@example.com"
            : toEmail;

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(
            _configuration["Smtp:FromName"],
            _configuration["Smtp:FromEmail"]));
        message.To.Add(new MailboxAddress(namaPembeli, destinationEmail));
        message.Subject = $"Tax Invoice Submission Received - {noInvoice}";

        var builder = new BodyBuilder
        {
            HtmlBody = $"""
                <p>Hello {namaPembeli},</p>
                <p>Here are the Tax Invoice submission details:</p>
                <p><strong>Name:</strong> {namaPembeli}<br />
                <strong>Receipt number:</strong> {noInvoice}<br />
                <strong>Transaction value:</strong> Rp{transactionValue:N0}</p>
                <p>We will process the Tax Invoice and email you back.</p>
                <p>If you have any problems, you can contact the email: taxrefund@delamibrands.com</p>
                <p>Thank you for your attention,</p>
                <p>Best regards,<br/>DelamiBrands</p>
                """,
        };
        message.Body = builder.ToMessageBody();

        using var client = new SmtpClient();
        client.Timeout = SmtpTimeoutMilliseconds;
        await client.ConnectAsync(
            _configuration["Smtp:Host"],
            int.Parse(_configuration["Smtp:Port"] ?? "587"),
            SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(
            _configuration["Smtp:User"],
            _configuration["Smtp:Pass"]);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }

    public async Task SendStoreInvoiceSubmittedEmailAsync(
        string toEmail,
        string noInvoice,
        string storeCode,
        string namaPembeli,
        decimal transactionValue)
    {
        var destinationEmail = ResolveStoreRecipient(toEmail);
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(
            _configuration["Smtp:FromName"],
            _configuration["Smtp:FromEmail"]));
        message.To.Add(new MailboxAddress("Store", destinationEmail));
        message.Subject = $"[STORE NOTIFICATION] Tax Invoice {noInvoice} - {storeCode}";

        var builder = new BodyBuilder
        {
            HtmlBody = $"""
                <p>Hello Team Store,</p>
                <p>Here are the Tax Invoice submission details:</p>
                <p><strong>Name:</strong> {namaPembeli}<br />
                <strong>Receipt number:</strong> {noInvoice}<br />
                <strong>Transaction value:</strong> Rp{transactionValue:N0}</p>
                <p>We will process the Tax Invoice and email you back.</p>
                <p>Thank you for your attention,</p>
                <p>Best regards,<br/>DelamiBrands</p>
                """,
        };
        message.Body = builder.ToMessageBody();

        using var client = new SmtpClient();
        client.Timeout = SmtpTimeoutMilliseconds;
        await client.ConnectAsync(
            _configuration["Smtp:Host"],
            int.Parse(_configuration["Smtp:Port"] ?? "587"),
            SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(_configuration["Smtp:User"], _configuration["Smtp:Pass"]);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }

    public async Task SendTaxInvoiceSubmittedEmailAsync(
        string noInvoice,
        string storeCode,
        string namaPembeli,
        decimal transactionValue)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(
            _configuration["Smtp:FromName"],
            _configuration["Smtp:FromEmail"]));
        message.To.Add(new MailboxAddress("Tim Tax", TaxTeamEmail));
        message.Subject = $"[TAX NOTIFICATION] Tax Invoice {noInvoice} - {storeCode}";

        var builder = new BodyBuilder
        {
            HtmlBody = $"""
                <p>Hello Team Tax,</p>
                <p>Here are the Tax Invoice submission details:</p>
                <p><strong>Name:</strong> {namaPembeli}<br />
                <strong>Receipt number:</strong> {noInvoice}<br />
                <strong>Transaction value:</strong> Rp{transactionValue:N0}</p>
                <p>We will process the Tax Invoice and email you back.</p>
                <p>Thank you for your attention,</p>
                <p>Best regards,<br/>DelamiBrands</p>
                """,
        };
        message.Body = builder.ToMessageBody();

        using var client = new SmtpClient();
        client.Timeout = SmtpTimeoutMilliseconds;
        await client.ConnectAsync(
            _configuration["Smtp:Host"],
            int.Parse(_configuration["Smtp:Port"] ?? "587"),
            SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(_configuration["Smtp:User"], _configuration["Smtp:Pass"]);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }

    public async Task SendNewInvoiceReminderAsync(string noInvoice, string storeCode)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(
            _configuration["Smtp:FromName"],
            _configuration["Smtp:FromEmail"]));
        message.To.Add(new MailboxAddress("Tim Tax", TaxTeamEmail));
        message.Subject = "[REMINDER] Permintaan Faktur Pajak (Tax Refund) – Batas Waktu H+1 23.59";

        var builder = new BodyBuilder
        {
            HtmlBody = """
                <p>Hi Team Tax! 👋</p>

                <p>Tax Refund Alert! Ada <strong>1</strong> permintaan Faktur Pajak yang masih menunggu untuk diproses.</p>

                <p>⏰ Heads up! Pengajuan ini harus selesai maksimal H+1 pukul 23.59 WIB.</p>

                <p>Biar semua pengajuan aman, nggak ada yang kelewat deadline, dan proses Tax Refund tetap lancar ✨</p>

                <p>One less pending task, one happy customer! 😉</p>

                <p>Thank you,<br/>Team</p>
                """,
        };
        message.Body = builder.ToMessageBody();

        using var client = new SmtpClient();
        client.Timeout = SmtpTimeoutMilliseconds;
        await client.ConnectAsync(
            _configuration["Smtp:Host"],
            int.Parse(_configuration["Smtp:Port"] ?? "587"),
            SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(_configuration["Smtp:User"], _configuration["Smtp:Pass"]);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }

    public async Task SendPendingInvoiceReminderAsync(int pendingCount)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(
            _configuration["Smtp:FromName"],
            _configuration["Smtp:FromEmail"]));
        message.To.Add(new MailboxAddress("Tim Tax", TaxTeamEmail));
        message.Subject = "[REMINDER] Permintaan Faktur Pajak (Tax Refund) – Batas Waktu H+1 23.59";

        var body = $"""
            <p>Hi Team Tax! 👋</p>

            <p>Tax Refund Alert! Ada <strong>{pendingCount}</strong> permintaan Faktur Pajak yang masih menunggu untuk diproses.</p>

            <p>⏰ Heads up! Pengajuan ini harus selesai maksimal H+1 pukul 23.59 WIB.</p>

            <p>Biar semua pengajuan aman, nggak ada yang kelewat deadline, dan proses Tax Refund tetap lancar ✨</p>

            <p>One less pending task, one happy customer! 😉</p>

            <p>Thank you,<br/>Team</p>
            """;

        var builder = new BodyBuilder { HtmlBody = body };
        message.Body = builder.ToMessageBody();

        using var client = new SmtpClient();
        client.Timeout = SmtpTimeoutMilliseconds;
        await client.ConnectAsync(
            _configuration["Smtp:Host"],
            int.Parse(_configuration["Smtp:Port"] ?? "587"),
            SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(_configuration["Smtp:User"], _configuration["Smtp:Pass"]);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}