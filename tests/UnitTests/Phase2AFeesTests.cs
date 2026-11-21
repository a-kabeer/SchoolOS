using SchoolOS.Modules.Fees.Application;
using SchoolOS.Modules.Fees.Domain;
using Shouldly;
using Xunit;

namespace SchoolOS.UnitTests;

public sealed class Phase2AFeesTests
{
    private static readonly Guid TenantId = Guid.CreateVersion7();
    private static readonly Guid SchoolId = Guid.CreateVersion7();
    private static readonly Guid CampusId = Guid.CreateVersion7();
    private static readonly Guid YearId = Guid.CreateVersion7();
    private static readonly Guid GradeId = Guid.CreateVersion7();
    private static readonly Guid HeadId = Guid.CreateVersion7();

    [Fact]
    public void FeeSchedule_ShouldApplySiblingDiscount_OnlyWhenHouseholdHasTwoOrMoreActiveSiblings()
    {
        var schedule = FeeSchedule.Create(TenantId, SchoolId, CampusId, YearId, GradeId, HeadId, 12500m, "PKR", siblingDiscountPercent: 10m);

        schedule.CalculateDiscountForSiblingCount(1).ShouldBe(0m);
        schedule.CalculateDiscountForSiblingCount(2).ShouldBe(1250m);
        schedule.CalculateDiscountForSiblingCount(3).ShouldBe(1250m);
    }

    [Fact]
    public void FeeChallan_ShouldEnforcePaymentOverpaymentReversalAndVoidInvariants()
    {
        var line = FeeChallanLine.Create(TenantId, HeadId, "Monthly Tuition", 10000m, 1000m);
        var challan = FeeChallan.Create(
            TenantId, SchoolId, CampusId, Guid.CreateVersion7(), null, YearId, GradeId,
            "2026-11", "CH-202611-001", "PKR", new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 10), [line]);

        challan.GrossAmount.ShouldBe(10000m);
        challan.DiscountAmount.ShouldBe(1000m);
        challan.NetAmount.ShouldBe(9000m);
        challan.BalanceAmount.ShouldBe(9000m);
        challan.Status.ShouldBe(ChallanStatus.Issued);

        // Partial payment
        challan.ApplyPayment(4000m);
        challan.Status.ShouldBe(ChallanStatus.PartiallyPaid);
        challan.PaidAmount.ShouldBe(4000m);
        challan.BalanceAmount.ShouldBe(5000m);

        // Overpayment rejected
        Should.Throw<InvalidOperationException>(() => challan.ApplyPayment(5001m));

        // Cannot void when payment exists
        Should.Throw<InvalidOperationException>(() => challan.Void("Duplicate"));

        // Final payment closes challan
        challan.ApplyPayment(5000m);
        challan.Status.ShouldBe(ChallanStatus.Paid);
        challan.BalanceAmount.ShouldBe(0m);

        // Reversing payment restores balance
        challan.ReversePayment(5000m);
        challan.Status.ShouldBe(ChallanStatus.PartiallyPaid);
        challan.BalanceAmount.ShouldBe(5000m);
    }

    [Fact]
    public void FeeReceipt_Reverse_ShouldRequireReason_AndPreventDoubleReversal()
    {
        var receipt = FeeReceipt.Create(
            TenantId, SchoolId, CampusId, Guid.CreateVersion7(), Guid.CreateVersion7(),
            "RCP-20261121-01", new DateOnly(2026, 11, 21), 4000m, "PKR",
            PaymentMethod.Cash, null, "cashier-1", DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(() => receipt.Reverse("  ", "admin", DateTimeOffset.UtcNow));

        receipt.Reverse("Cheque bounced", "admin", DateTimeOffset.UtcNow);
        receipt.Status.ShouldBe(ReceiptStatus.Reversed);
        receipt.ReversalReason.ShouldBe("Cheque bounced");

        Should.Throw<InvalidOperationException>(() => receipt.Reverse("Second attempt", "admin", DateTimeOffset.UtcNow));
    }

    [Fact]
    public void ComputeAging_ShouldBucketOutstandingChallansAccurately()
    {
        FeeChallan Make(string num, DateOnly due, decimal amt) =>
            FeeChallan.Create(
                TenantId, SchoolId, CampusId, Guid.CreateVersion7(), null, YearId, GradeId,
                "2026-11", num, "PKR", due.AddDays(-10), due,
                [FeeChallanLine.Create(TenantId, HeadId, "Tuition", amt)]);

        var asOf = new DateOnly(2026, 12, 20);
        var aging = FeesService.ComputeAging(
        [
            Make("C1", new DateOnly(2026, 12, 25), 1000m), // Current
            Make("C2", new DateOnly(2026, 12, 1), 2000m),  // 19 days -> 1-30
            Make("C3", new DateOnly(2026, 11, 5), 3000m),  // 45 days -> 31-60
            Make("C4", new DateOnly(2026, 10, 1), 4000m),  // 80 days -> 61+
        ], asOf);

        aging.OutstandingChallanCount.ShouldBe(4);
        aging.TotalOutstanding.ShouldBe(10000m);
        aging.CurrentAmount.ShouldBe(1000m);
        aging.Days1To30Amount.ShouldBe(2000m);
        aging.Days31To60Amount.ShouldBe(3000m);
        aging.Days61PlusAmount.ShouldBe(4000m);
    }
}
