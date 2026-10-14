using System.Globalization;

namespace ABR.Infrastructure.Formatting;

/// <summary>Formats stored rupee amounts for display as amount÷1000 with no grouping.</summary>
public static class DisplayAmountFormatter
{
    public static string Format(decimal? amount)
    {
        if (!amount.HasValue) return string.Empty;
        return Format(amount.Value);
    }

    public static string Format(decimal amount)
    {
        var scaled = amount / 1000m;
        // Money has 2 decimal places → up to 5 after ÷1000
        var text = scaled.ToString("0.#####", CultureInfo.InvariantCulture);
        return text == "-0" ? "0" : text;
    }

    /// <summary>Numeric value for Excel cells (still ÷1000).</summary>
    public static decimal Scale(decimal amount) => amount / 1000m;

    public static decimal? Scale(decimal? amount) => amount.HasValue ? amount.Value / 1000m : null;
}
