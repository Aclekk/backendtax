namespace TaxRefund.Api.Modules.Traveler.DTOs;

public class CreateTravelerRequest
{
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
}

public class TravelerResponse
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
    public string SourceChannel { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class TravelerDetailResponse : TravelerResponse
{
}

public class TravelerHistoryListResponse
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string Nationality { get; set; } = string.Empty;
    public string? PassportNumber { get; set; }
    public string? SourceChannel { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? LastActivityAt { get; set; }
}
