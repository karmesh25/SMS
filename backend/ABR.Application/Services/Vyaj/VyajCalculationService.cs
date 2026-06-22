namespace ABR.Application.Services.Vyaj;

public static class VyajCalculationService
{
    public static decimal CalculateGrossVyaj(
        decimal principal,
        decimal ratePercent,
        string rateBasis,
        DateOnly startDate,
        DateOnly asOfDate)
    {
        if (principal <= 0 || ratePercent <= 0)
            return 0;

        var rate = ratePercent / 100m;

        return rateBasis switch
        {
            "flat" => RoundMoney(principal * rate),
            "month" => RoundMoney(principal * rate * MonthsBetween(startDate, asOfDate)),
            "year" => RoundMoney(principal * rate * DaysBetween(startDate, asOfDate) / 365m),
            "day" => RoundMoney(principal * rate * DaysBetween(startDate, asOfDate)),
            _ => RoundMoney(principal * rate * MonthsBetween(startDate, asOfDate))
        };
    }

    public static VyajEntryTotals CalculateEntryTotals(
        decimal principal,
        decimal ratePercent,
        string rateBasis,
        DateOnly startDate,
        IEnumerable<(decimal Amount, string PaymentType)> payments,
        DateOnly? asOfDate = null)
    {
        var asOf = asOfDate ?? DateOnly.FromDateTime(DateTime.Now);
        var grossVyaj = CalculateGrossVyaj(principal, ratePercent, rateBasis, startDate, asOf);

        decimal interestPaid = 0;
        decimal principalPaid = 0;

        foreach (var (amount, paymentType) in payments)
        {
            if (string.Equals(paymentType, "principal", StringComparison.OrdinalIgnoreCase))
                principalPaid += amount;
            else
                interestPaid += amount;
        }

        return new VyajEntryTotals
        {
            GrossVyaj = RoundMoney(grossVyaj),
            InterestPaid = RoundMoney(interestPaid),
            PrincipalPaid = RoundMoney(principalPaid),
            VyajDue = RoundMoney(Math.Max(0, grossVyaj - interestPaid)),
            PrincipalDue = RoundMoney(Math.Max(0, principal - principalPaid))
        };
    }

    public static int DaysBetween(DateOnly start, DateOnly end)
    {
        var days = end.DayNumber - start.DayNumber;
        return Math.Max(0, days);
    }

    public static decimal MonthsBetween(DateOnly start, DateOnly end)
    {
        if (end < start)
            return 0;

        var months = (end.Year - start.Year) * 12 + (end.Month - start.Month);
        if (end.Day < start.Day)
            months--;

        var anchor = start.AddMonths(months);
        var remDays = end.DayNumber - anchor.DayNumber;
        return Math.Max(0, months) + remDays / 30m;
    }

    public static decimal RoundMoney(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}

public sealed class VyajEntryTotals
{
    public decimal GrossVyaj { get; init; }
    public decimal InterestPaid { get; init; }
    public decimal PrincipalPaid { get; init; }
    public decimal VyajDue { get; init; }
    public decimal PrincipalDue { get; init; }
}
