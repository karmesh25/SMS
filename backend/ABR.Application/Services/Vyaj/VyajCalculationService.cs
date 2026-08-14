namespace ABR.Application.Services.Vyaj;

public static class VyajCalculationService
{
    public static decimal CalculateGrossVyaj(
        decimal principal,
        decimal ratePercent,
        string rateBasis,
        DateOnly startDate,
        DateOnly asOfDate,
        int? ratePeriodMonths = null)
    {
        return CalculateReducingGrossVyaj(
            principal,
            ratePercent,
            rateBasis,
            startDate,
            asOfDate,
            Array.Empty<(decimal Amount, DateOnly PaymentDate)>(),
            ratePeriodMonths);
    }

    /// <summary>
    /// Accrues vyaj on reducing principal: principal payments create time segments.
    /// Month-period caps (3/6/9) are measured from the original <paramref name="startDate"/>.
    /// Flat rate applies once to outstanding principal as of <paramref name="asOfDate"/>.
    /// </summary>
    public static decimal CalculateReducingGrossVyaj(
        decimal principal,
        decimal ratePercent,
        string rateBasis,
        DateOnly startDate,
        DateOnly asOfDate,
        IEnumerable<(decimal Amount, DateOnly PaymentDate)> principalPayments,
        int? ratePeriodMonths = null)
    {
        if (principal <= 0 || ratePercent <= 0)
            return 0;

        var basis = rateBasis?.ToLowerInvariant() ?? "month";
        var rate = ratePercent / 100m;

        var paymentsByDate = principalPayments
            .Where(p => p.Amount > 0 && p.PaymentDate <= asOfDate)
            .GroupBy(p => p.PaymentDate)
            .OrderBy(g => g.Key)
            .Select(g => (Date: g.Key, Amount: g.Sum(x => x.Amount)))
            .ToList();

        if (string.Equals(basis, "flat", StringComparison.Ordinal))
        {
            var outstanding = principal;
            foreach (var (_, amount) in paymentsByDate)
                outstanding = Math.Max(0, outstanding - amount);
            return RoundMoney(outstanding * rate);
        }

        decimal gross = 0;
        var outstandingPrincipal = principal;
        var cursor = startDate;

        foreach (var (payDate, amount) in paymentsByDate)
        {
            if (payDate > cursor)
            {
                gross += AccrueSegment(
                    outstandingPrincipal, rate, basis, startDate, cursor, payDate, ratePeriodMonths);
            }

            outstandingPrincipal = Math.Max(0, outstandingPrincipal - amount);
            if (payDate > cursor)
                cursor = payDate;
        }

        if (asOfDate > cursor)
        {
            gross += AccrueSegment(
                outstandingPrincipal, rate, basis, startDate, cursor, asOfDate, ratePeriodMonths);
        }

        return RoundMoney(gross);
    }

    public static VyajEntryTotals CalculateEntryTotals(
        decimal principal,
        decimal ratePercent,
        string rateBasis,
        DateOnly startDate,
        IEnumerable<(decimal Amount, string PaymentType)> payments,
        DateOnly? asOfDate = null,
        int? ratePeriodMonths = null)
    {
        var asOf = asOfDate ?? DateOnly.FromDateTime(DateTime.Now);
        var dated = payments.Select(p => (p.Amount, p.PaymentType, PaymentDate: startDate));
        return CalculateEntryTotals(principal, ratePercent, rateBasis, startDate, dated, asOf, ratePeriodMonths);
    }

    public static VyajEntryTotals CalculateEntryTotals(
        decimal principal,
        decimal ratePercent,
        string rateBasis,
        DateOnly startDate,
        IEnumerable<(decimal Amount, string PaymentType, DateOnly PaymentDate)> payments,
        DateOnly? asOfDate = null,
        int? ratePeriodMonths = null)
    {
        var asOf = asOfDate ?? DateOnly.FromDateTime(DateTime.Now);
        var paymentList = payments.ToList();

        var principalPays = paymentList
            .Where(p => string.Equals(p.PaymentType, "principal", StringComparison.OrdinalIgnoreCase))
            .Select(p => (p.Amount, p.PaymentDate));

        var grossVyaj = CalculateReducingGrossVyaj(
            principal, ratePercent, rateBasis, startDate, asOf, principalPays, ratePeriodMonths);

        decimal interestPaid = 0;
        decimal principalPaid = 0;

        foreach (var (amount, paymentType, _) in paymentList)
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

    private static decimal AccrueSegment(
        decimal outstandingPrincipal,
        decimal rate,
        string rateBasis,
        DateOnly entryStart,
        DateOnly segmentStart,
        DateOnly segmentEnd,
        int? ratePeriodMonths)
    {
        if (outstandingPrincipal <= 0 || segmentEnd <= segmentStart)
            return 0;

        return rateBasis switch
        {
            "month" => outstandingPrincipal * rate *
                       Math.Max(0,
                           ResolveMonthFactor(entryStart, segmentEnd, ratePeriodMonths) -
                           ResolveMonthFactor(entryStart, segmentStart, ratePeriodMonths)),
            "year" => outstandingPrincipal * rate * DaysBetween(segmentStart, segmentEnd) / 365m,
            "day" => outstandingPrincipal * rate * DaysBetween(segmentStart, segmentEnd),
            _ => outstandingPrincipal * rate *
                 Math.Max(0,
                     ResolveMonthFactor(entryStart, segmentEnd, ratePeriodMonths) -
                     ResolveMonthFactor(entryStart, segmentStart, ratePeriodMonths))
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

    public static decimal ResolveMonthFactor(DateOnly startDate, DateOnly asOfDate, int? ratePeriodMonths)
    {
        var elapsed = MonthsBetween(startDate, asOfDate);
        if (ratePeriodMonths is 3 or 6 or 9)
            return Math.Min(elapsed, ratePeriodMonths.Value);
        return elapsed;
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
