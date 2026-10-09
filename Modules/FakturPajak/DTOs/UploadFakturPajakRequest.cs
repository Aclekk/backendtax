namespace TaxRefund.Api.Modules.FakturPajak.DTOs;

public class UploadFakturPajakRequest
{
    public IFormFile File { get; set; } = null!;
    public string? NomorFakturPajak { get; set; }
}