using Booking.Domain.Accounting;

namespace Booking.Application.Tests.Accounting;

public sealed class AccountingDomainTests
{
    [Fact]
    public void Split_CalculatesVatAndNet_WithCommercialRounding()
    {
        var split = new AccountingEntrySplit(null, 21, 121m, 0m, AccountingEntryType.Revenue);

        Assert.Equal(100m, split.NetAmount);
        Assert.Equal(21m, split.VatAmount);
    }

    [Fact]
    public void ExpenseSplit_AllowsPartiallyDeductibleVat()
    {
        var split = new AccountingEntrySplit(null, 21, 121m, 10m, AccountingEntryType.Expense);

        Assert.Equal(10m, split.DeductibleVatAmount);
    }

    [Fact]
    public void Split_RejectsUnsupportedVatRate()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new AccountingEntrySplit(null, 6, 106m, 0m, AccountingEntryType.Revenue));
    }

    [Fact]
    public void ConfirmedEntry_IsImmutable()
    {
        var entry = Entry();
        entry.Confirm(DateTime.UtcNow);

        Assert.Throws<InvalidOperationException>(() =>
            entry.UpdateDraft(AccountingEntryType.Revenue, DateOnly.FromDateTime(DateTime.Today), "Changed", [new AccountingEntrySplit(null, 9, 109m, 0m, AccountingEntryType.Revenue)]));
    }

    [Fact]
    public void Reversal_HasOppositeAmounts()
    {
        var entry = Entry();
        entry.Confirm(DateTime.UtcNow);

        var reversal = entry.CreateReversal(DateTime.UtcNow);

        Assert.Equal(-121m, reversal.GrossAmount);
        Assert.Equal(-21m, reversal.Splits.Single().VatAmount);
        Assert.Equal(entry.Id, reversal.CorrectionOfEntryId);
    }

    [Fact]
    public void SourceTransaction_RejectsInvalidCurrency()
    {
        Assert.Throws<ArgumentException>(() => new AccountingSourceTransaction(
            Guid.NewGuid(), AccountingSourceKind.BankCsv, "id", "fingerprint", DateOnly.FromDateTime(DateTime.Today), "test", 1m, "EURO", "{}", DateTime.UtcNow));
    }

    private static AccountingEntry Entry() => new(
        Guid.NewGuid(),
        AccountingEntryType.Revenue,
        DateOnly.FromDateTime(DateTime.Today),
        "Omzet",
        [new AccountingEntrySplit(null, 21, 121m, 0m, AccountingEntryType.Revenue)],
        DateTime.UtcNow);
}
