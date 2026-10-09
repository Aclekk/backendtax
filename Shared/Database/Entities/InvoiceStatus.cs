namespace TaxRefund.Api.Shared.Database.Entities;

public enum InvoiceStatus
{
    New,
    Downloaded,
    FpUploaded,
    Sent,
    EmailFailed
}