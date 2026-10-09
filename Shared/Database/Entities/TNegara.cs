namespace TaxRefund.Api.Shared.Database.Entities;

public class TNegara
{
    public int Id { get; set; }

    public string Kode { get; set; } = null!;

    public string Nama { get; set; } = null!;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}