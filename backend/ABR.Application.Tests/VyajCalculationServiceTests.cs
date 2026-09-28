using ABR.Application.Services.Vyaj;

namespace ABR.Application.Tests;

public class VyajCalculationServiceTests
{
    [Fact]
    public void FlatRate_ReturnsPrincipalTimesRate()
    {
        var gross = VyajCalculationService.CalculateGrossVyaj(100_000, 5, "flat", new DateOnly(2024, 1, 1), new DateOnly(2024, 6, 1));
        Assert.Equal(5_000, gross);
    }

    [Fact]
    public void MonthRate_UsesFractionalMonths()
    {
        var gross = VyajCalculationService.CalculateGrossVyaj(100_000, 2, "month", new DateOnly(2024, 1, 15), new DateOnly(2024, 4, 14));
        Assert.Equal(6_000, gross);
    }

    [Fact]
    public void MonthRate_PartialMonthAccruesByDays()
    {
        var gross = VyajCalculationService.CalculateGrossVyaj(100_000, 2, "month", new DateOnly(2024, 1, 15), new DateOnly(2024, 1, 20));
        Assert.Equal(333.33m, gross);
    }

    [Fact]
    public void MonthRate_FiveDaysOnFortyLakh()
    {
        var gross = VyajCalculationService.CalculateGrossVyaj(4_000_000, 2, "month", new DateOnly(2024, 6, 17), new DateOnly(2024, 6, 22));
        Assert.Equal(13_333.33m, gross);
    }

    [Fact]
    public void YearRate_UsesDaysOver365()
    {
        var gross = VyajCalculationService.CalculateGrossVyaj(365_000, 10, "year", new DateOnly(2024, 1, 1), new DateOnly(2025, 1, 1));
        Assert.Equal(36_600, gross);
    }

    [Fact]
    public void DayRate_UsesDaysElapsed()
    {
        var gross = VyajCalculationService.CalculateGrossVyaj(10_000, 1, "day", new DateOnly(2024, 1, 1), new DateOnly(2024, 1, 31));
        Assert.Equal(3_000, gross);
    }

    [Fact]
    public void ZeroPrincipal_ReturnsZero()
    {
        var gross = VyajCalculationService.CalculateGrossVyaj(0, 2, "month", new DateOnly(2024, 1, 1), new DateOnly(2024, 6, 1));
        Assert.Equal(0, gross);
    }

    [Fact]
    public void EntryTotals_SubtractsInterestPayments_WithoutPrincipalReduction()
    {
        var payments = new[] { (500m, "interest", new DateOnly(2024, 2, 1)) };
        var totals = VyajCalculationService.CalculateEntryTotals(
            100_000, 2, "month",
            new DateOnly(2024, 1, 1),
            payments,
            new DateOnly(2024, 4, 1));

        Assert.Equal(6_000, totals.GrossVyaj);
        Assert.Equal(500, totals.InterestPaid);
        Assert.Equal(0, totals.PrincipalPaid);
        Assert.Equal(5_500, totals.VyajDue);
        Assert.Equal(100_000, totals.PrincipalDue);
    }

    [Fact]
    public void ReducingBalance_FirstEmiOnStart_AccruesOnRemaining()
    {
        // 50L principal, first EMI 5L on start → remaining 45L @ 2%/month for 3 months
        var payments = new[] { (500_000m, "principal", new DateOnly(2024, 1, 1)) };
        var totals = VyajCalculationService.CalculateEntryTotals(
            5_000_000, 2, "month",
            new DateOnly(2024, 1, 1),
            payments,
            new DateOnly(2024, 4, 1),
            ratePeriodMonths: 3);

        Assert.Equal(270_000, totals.GrossVyaj); // 45L * 2% * 3
        Assert.Equal(500_000, totals.PrincipalPaid);
        Assert.Equal(4_500_000, totals.PrincipalDue);
        Assert.Equal(270_000, totals.VyajDue);
    }

    [Fact]
    public void ReducingBalance_PrincipalPaidMidPeriod_SegmentsInterest()
    {
        // 100k @ 2%/month: Jan1–Feb1 on 100k (1 month = 2000), then pay 20k, Feb1–Apr1 on 80k (2 months = 3200)
        var payments = new[] { (20_000m, "principal", new DateOnly(2024, 2, 1)) };
        var totals = VyajCalculationService.CalculateEntryTotals(
            100_000, 2, "month",
            new DateOnly(2024, 1, 1),
            payments,
            new DateOnly(2024, 4, 1));

        Assert.Equal(5_200, totals.GrossVyaj);
        Assert.Equal(80_000, totals.PrincipalDue);
    }

    [Fact]
    public void MonthPeriod_NoLongerHardCapsAccrual_ContinuesPastInterval()
    {
        var payments = new[] { (500_000m, "principal", new DateOnly(2024, 1, 1)) };
        var at3Months = VyajCalculationService.CalculateEntryTotals(
            5_000_000, 2, "month",
            new DateOnly(2024, 1, 1),
            payments,
            new DateOnly(2024, 4, 1),
            ratePeriodMonths: 3);
        var at6Months = VyajCalculationService.CalculateEntryTotals(
            5_000_000, 2, "month",
            new DateOnly(2024, 1, 1),
            payments,
            new DateOnly(2024, 7, 1),
            ratePeriodMonths: 3);

        Assert.Equal(270_000, at3Months.GrossVyaj); // 45L * 2% * 3
        Assert.Equal(540_000, at6Months.GrossVyaj); // 45L * 2% * 6 — accrual continues
    }

    [Fact]
    public void ResolveCompoundPeriodMonths_UsesRatePeriodOrMonthlyDefault()
    {
        Assert.Equal(3, VyajCalculationService.ResolveCompoundPeriodMonths("month", 3));
        Assert.Equal(6, VyajCalculationService.ResolveCompoundPeriodMonths("month", 6));
        Assert.Equal(1, VyajCalculationService.ResolveCompoundPeriodMonths("month", null));
        Assert.Equal(1, VyajCalculationService.ResolveCompoundPeriodMonths("year", null));
        Assert.Equal(1, VyajCalculationService.ResolveCompoundPeriodMonths("flat", null));
    }

    [Fact]
    public void Flat_CapitalizeRaisesPrincipal_AndGrossUsesNewOutstanding()
    {
        // Flat 1.5% on 1_000_000 = 15_000. Capitalize unpaid 10_000 (after 5k interest paid).
        var payments = new[]
        {
            (5_000m, "interest", new DateOnly(2024, 1, 15)),
            (10_000m, "capitalize", new DateOnly(2024, 1, 31))
        };
        var totals = VyajCalculationService.CalculateEntryTotals(
            1_000_000, 1.5m, "flat",
            new DateOnly(2024, 1, 1),
            payments,
            new DateOnly(2024, 2, 1));

        Assert.Equal(1_010_000, totals.PrincipalDue);
        Assert.Equal(15_150, totals.GrossVyaj); // 1_010_000 * 1.5%
        Assert.Equal(15_000, totals.InterestPaid); // 5k interest + 10k capitalize
        Assert.Equal(150, totals.VyajDue);
    }

    [Fact]
    public void EntryTotals_VyajDueNeverNegative()
    {
        var payments = new[] { (10_000m, "interest", new DateOnly(2024, 1, 1)) };
        var totals = VyajCalculationService.CalculateEntryTotals(
            50_000, 1, "flat",
            new DateOnly(2024, 1, 1),
            payments);

        Assert.Equal(0, totals.VyajDue);
    }

    [Fact]
    public void MonthsBetween_HandlesPartialMonth()
    {
        Assert.Equal(26m / 30m, VyajCalculationService.MonthsBetween(new DateOnly(2024, 3, 15), new DateOnly(2024, 4, 10)));
        Assert.Equal(1, VyajCalculationService.MonthsBetween(new DateOnly(2024, 3, 15), new DateOnly(2024, 4, 15)));
    }

    [Fact]
    public void DaysBetween_NeverNegative()
    {
        Assert.Equal(0, VyajCalculationService.DaysBetween(new DateOnly(2024, 5, 1), new DateOnly(2024, 4, 1)));
        Assert.Equal(30, VyajCalculationService.DaysBetween(new DateOnly(2024, 1, 1), new DateOnly(2024, 1, 31)));
    }

    [Fact]
    public void Capitalize_SettlesVyajDue_AndRaisesPrincipal()
    {
        // Jan–Feb: 100k @ 2% = 2000 accrued. Capitalize 2000 on Jan 31.
        // Feb–Mar: accrues on 102k = 2040. Total gross = 2000 + 2040 = 4040.
        // Interest paid includes capitalize 2000 → due = 2040. Principal due = 102000.
        var payments = new[] { (2_000m, "capitalize", new DateOnly(2024, 1, 31)) };
        var totals = VyajCalculationService.CalculateEntryTotals(
            100_000, 2, "month",
            new DateOnly(2024, 1, 1),
            payments,
            new DateOnly(2024, 3, 1));

        Assert.Equal(4_040, totals.GrossVyaj);
        Assert.Equal(2_000, totals.InterestPaid);
        Assert.Equal(2_040, totals.VyajDue);
        Assert.Equal(102_000, totals.PrincipalDue);
    }

    [Fact]
    public void Capitalize_DoesNotDoubleCountInVyajDue()
    {
        var payments = new[] { (2_000m, "capitalize", new DateOnly(2024, 1, 31)) };
        var totals = VyajCalculationService.CalculateEntryTotals(
            100_000, 2, "month",
            new DateOnly(2024, 1, 1),
            payments,
            new DateOnly(2024, 1, 31));

        Assert.Equal(2_000, totals.GrossVyaj);
        Assert.Equal(0, totals.VyajDue);
        Assert.Equal(102_000, totals.PrincipalDue);
    }
}
