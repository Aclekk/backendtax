namespace TaxRefund.Api.Shared.Common;

public static class StoreTaxRules
{
    public static bool IsBatamStore(string? storeCode, string? storeName = null, string? storeCity = null, string? province = null)
    {
        foreach (var candidate in new[] { storeCode, storeName, storeCity, province })
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                continue;
            }

            var normalized = candidate.Trim();
            var compact = string.Concat(normalized.Where(char.IsLetterOrDigit)).ToUpperInvariant();

            if (compact.Contains("BATAM") || compact == "BTA" || compact.StartsWith("BTA", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
