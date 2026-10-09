using System.ComponentModel.DataAnnotations;

namespace TaxRefund.Api.Modules.Invoice.DTOs;

public class CreateInvoiceRequest
{
    public string StoreCode { get; set; } = string.Empty;
    public string StoreEmail { get; set; } = string.Empty;
    public string NoInvoice { get; set; } = string.Empty;
    public string NamaPembeli { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email pembeli wajib diisi.")]
    [EmailAddress(ErrorMessage = "Format email pembeli tidak valid.")]
    public string EmailPembeli { get; set; } = string.Empty;
    public string NegaraPembeli { get; set; } = string.Empty;
    public string NomorDokumenPembeli { get; set; } = string.Empty;
    public string AlamatPembeli { get; set; } = string.Empty;
}