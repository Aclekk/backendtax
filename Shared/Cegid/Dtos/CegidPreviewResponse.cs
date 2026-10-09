namespace TaxRefund.Api.Modules.Invoice.DTOs;

public class CegidPreviewItem
{
    public string NamaBrg { get; set; } = string.Empty;
    public string HargaSatuan { get; set; } = string.Empty;
    public string JumlahBrgJasa { get; set; } = string.Empty;
    public string HargaTotal { get; set; } = string.Empty;
    public string TotalDiskon { get; set; } = string.Empty;
    public string Dpp { get; set; } = string.Empty;
    public string Ppn { get; set; } = string.Empty;
}

public class CegidStoreOption
{
    public string StoreCode { get; set; } = string.Empty;
    public string StoreName { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public decimal Subtotal { get; set; }
}

public class CegidPreviewResponse
{
    public string StoreCode { get; set; } = string.Empty;
    public string StoreName { get; set; } = string.Empty;
    public string ReceiptNo { get; set; } = string.Empty;
    public DateTime TransDate { get; set; }

    public string NamaMember { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Address { get; set; }
    public List<string> AvailableStoreCodes { get; set; } = [];
    public List<CegidStoreOption> StoreOptions { get; set; } = [];

    public int ItemCount { get; set; }
    public decimal Subtotal { get; set; }

    public bool AlreadyExists { get; set; }

    public List<CegidPreviewItem> Items { get; set; } = [];
}