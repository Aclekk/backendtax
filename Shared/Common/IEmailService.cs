namespace TaxRefund.Api.Shared.Common;

public interface IEmailService
{
    Task SendFakturPajakEmailAsync(string toEmail, string namaPembeli, string noInvoice, string attachmentAbsolutePath, string attachmentFileName);
    Task SendCoreTaxFailureEmailAsync(string toEmail, string namaPembeli, string noInvoice);
    Task SendInvoiceReceivedEmailAsync(string toEmail, string namaPembeli, string noInvoice, string storeCode, decimal transactionValue);
    Task SendStoreInvoiceSubmittedEmailAsync(string toEmail, string noInvoice, string storeCode, string namaPembeli, decimal transactionValue);
    Task SendTaxInvoiceSubmittedEmailAsync(string noInvoice, string storeCode, string namaPembeli, decimal transactionValue);

    // BARU — reminder untuk tim Tax
    Task SendNewInvoiceReminderAsync(string noInvoice, string storeCode);
    Task SendPendingInvoiceReminderAsync(int pendingCount);
}