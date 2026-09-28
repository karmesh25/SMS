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
            Array.Empty<(decimal Amount, DateOnly PaymentDate, int PrincipalDeltaSign)>(),
            ratePeriodMonths);
    }

    /// <summary>
    /// Accrues vyaj on reducing/increasing principal.
    /// Principal payments use <c>PrincipalDeltaSign = -1</c>; capitalize uses <c>+1</c>.
    /// <paramref name="ratePeriodMonths"/> is the compound/due interval hint only — accrual is not hard-capped.
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
        var events = principalPayments.Select(p => (p.Amount, p.PaymentDate, PrincipalDeltaSign: -1));
        return CalculateReducingGrossVyaj(principal, ratePercent, rateBasis, startDate, asOfDate, events, ratePeriodMonths);
    }

    public static decimal CalculateReducingGrossVyaj(
        decimal principal,
        decimal ratePercent,
        string rateBasis,
        DateOnly startDate,
        DateOnly asOfDate,
        IEnumerable<(decimal Amount, DateOnly PaymentDate, int PrincipalDeltaSign)> principalEvents,
        int? ratePeriodMonths = null)
    {
        if (principal <= 0 || ratePercent <= 0)
            return 0;

        var basis = rateBasis?.ToLowerInvariant() ?? "month";
        var rate = ratePercent / 100m;

        var paymentsByDate = principalEvents
            .Where(p => p.Amount > 0 && p.PaymentDate <= asOfDate && p.PrincipalDeltaSign != 0)
            .GroupBy(p => p.PaymentDate)
            .OrderBy(g => g.Key)
            .Select(g => (
                Date: g.Key,
                Delta: g.Sum(x => x.Amount * x.PrincipalDeltaSign)))
            .ToList();

        if (string.Equals(basis, "flat", StringComparison.Ordinal))
        {
            var outstanding = principal;
            foreach (var (_, delta) in paymentsByDate)
                outstanding = Math.Max(0, outstanding + delta);
            return RoundMoney(outstanding * rate);
        }

        decimal gross = 0;
        var outstandingPrincipal = principal;
        var cursor = startDate;

        foreach (var (payDate, delta) in paymentsByDate)
        {
            if (payDate > cursor)
            {
                gross += AccrueSegment(
                    outstandingPrincipal, rate, basis, startDate, cursor, payDate, ratePeriodMonths);
            }

            outstandingPrincipal = Math.Max(0, outstandingPrincipal + delta);
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

        var principalEvents = paymentList
            .Where(p =>
                string.Equals(p.PaymentType, "principal", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(p.PaymentType, "capitalize", StringComparison.OrdinalIgnoreCase))
            .Select(p => (
                p.Amount,
                p.PaymentDate,
                PrincipalDeltaSign: string.Equals(p.PaymentType, "capitalize", StringComparison.OrdinalIgnoreCase) ? 1 : -1));

        var grossVyaj = CalculateReducingGrossVyaj(
            principal, ratePercent, rateBasis, startDate, asOf, principalEvents, ratePeriodMonths);

        decimal interestPaid = 0;
        decimal principalPaid = 0;
        decimal capitalized = 0;

        foreach (var (amount, paymentType, _) in paymentList)
        {
            if (string.Equals(paymentType, "principal", StringComparison.OrdinalIgnoreCase))
                principalPaid += amount;
            else if (string.Equals(paymentType, "capitalize", StringComparison.OrdinalIgnoreCase))
            {
                capitalized += amount;
                interestPaid += amount; // settles vyaj due
            }
            else
                interestPaid += amount;
        }

        return new VyajEntryTotals
        {
            GrossVyaj = RoundMoney(grossVyaj),
            InterestPaid = RoundMoney(interestPaid),
            PrincipalPaid = RoundMoney(principalPaid),
            VyajDue = RoundMoney(Math.Max(0, grossVyaj - interestPaid)),
            PrincipalDue = RoundMoney(Math.Max(0, principal + capitalized - principalPaid))
        };
    }

    /// <summary>
    /// Accrued vyaj for one calendar month segment on then-outstanding principal (before month-end capitalize).
    /// </summary>
    public static decimal CalculateMonthAccrual(
        decimal principal,
        decimal ratePercent,
        string rateBasis,
        DateOnly startDate,
        DateOnly monthStart,
        DateOnly monthEnd,
        IEnumerable<(decimal Amount, string PaymentType, DateOnly PaymentDate)> payments,
        int? ratePeriodMonths = null)
    {
        if (monthEnd <= monthStart)
            return 0;

        var beforeEnd = payments
            .Where(p => p.PaymentDate < monthEnd)
            .ToList();

        var totalsAtStart = CalculateEntryTotals(
            principal, ratePercent, rateBasis, startDate, beforeEnd, monthStart, ratePeriodMonths);
        var totalsAtEnd = CalculateEntryTotals(
            principal, ratePercent, rateBasis, startDate, beforeEnd, monthEnd, ratePeriodMonths);

        // Gross difference for the month, using outstanding path already in reducing calc
        return RoundMoney(Math.Max(0, totalsAtEnd.GrossVyaj - totalsAtStart.GrossVyaj));
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
            "day" or "custom" => outstandingPrincipal * rate * DaysBetween(segmentStart, segmentEnd),
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
        // ratePeriodMonths is the due/compound interval used by capitalization — not an accrual cap.
        _ = ratePeriodMonths;
        return MonthsBetween(startDate, asOfDate);
    }

    /// <summary>
    /// Due/compound interval in months: 3/6/9 when set, otherwise 1 (monthly).
    /// </summary>
    public static int ResolveCompoundPeriodMonths(string? rateBasis, int? ratePeriodMonths)
    {
        if (ratePeriodMonths is 3 or 6 or 9)
            return ratePeriodMonths.Value;
        return 1;
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
