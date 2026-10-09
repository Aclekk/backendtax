namespace TaxRefund.Api.Shared.Drs;

public static class MasterBarangFpKey
{
    public static string? FromBrandAndCategory(string? brand, string? category)
    {
        if (string.IsNullOrWhiteSpace(brand) || string.IsNullOrWhiteSpace(category))
        {
            return null;
        }

        return $"{NormalizePart(brand)}-{NormalizePart(category)}";
    }

    public static string Normalize(string value)
    {
        return value.Trim().ToUpperInvariant();
    }

    private static string NormalizePart(string value)
    {
        return Normalize(value);
    }
}
