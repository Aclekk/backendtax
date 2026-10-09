namespace TaxRefund.Api.Shared.Database.Entities;

public class TTraveler
{
    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? Address { get; set; }

    public string Nationality { get; set; } = string.Empty;

    public string PassportNumber { get; set; } = string.Empty;

    public string? PassportType { get; set; }

    public DateOnly? BirthDate { get; set; }

    public string? BirthPlace { get; set; }

    public DateOnly? PassportExpiryDate { get; set; }

    public string SourceChannel { get; set; } = "tourist_self_service";

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    // Navigation: invoices for this traveler/customer
    public virtual ICollection<TInvoice> Invoices { get; set; } = new List<TInvoice>();
}
