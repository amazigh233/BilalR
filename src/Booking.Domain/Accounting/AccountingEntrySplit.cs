namespace Booking.Domain.Accounting;

public sealed class AccountingEntrySplit
{
    private AccountingEntrySplit()
    {
    }

    public AccountingEntrySplit(
        Guid? categoryId,
        int vatRate,
        decimal grossAmount,
        decimal deductibleVatAmount,
        AccountingEntryType entryType)
    {
        if (vatRate is not (0 or 9 or 21))
        {
            throw new ArgumentOutOfRangeException(nameof(vatRate), "VAT rate must be 0, 9 or 21.");
        }

        if (entryType == AccountingEntryType.Transfer && vatRate != 0)
        {
            throw new ArgumentException("Transfers cannot contain VAT.", nameof(vatRate));
        }

        var netAmount = vatRate == 0
            ? grossAmount
            : Math.Round(grossAmount * 100m / (100m + vatRate), 2, MidpointRounding.AwayFromZero);
        var vatAmount = grossAmount - netAmount;

        if (entryType != AccountingEntryType.Expense && deductibleVatAmount != 0)
        {
            throw new ArgumentException("Only expense VAT can be deductible.", nameof(deductibleVatAmount));
        }

        if (Math.Abs(deductibleVatAmount) > Math.Abs(vatAmount) ||
            Math.Sign(deductibleVatAmount) != 0 && Math.Sign(deductibleVatAmount) != Math.Sign(vatAmount))
        {
            throw new ArgumentOutOfRangeException(
                nameof(deductibleVatAmount),
                "Deductible VAT must be between zero and the calculated VAT amount.");
        }

        CategoryId = categoryId;
        VatRate = vatRate;
        GrossAmount = grossAmount;
        NetAmount = netAmount;
        VatAmount = vatAmount;
        DeductibleVatAmount = deductibleVatAmount;
    }

    public Guid? CategoryId { get; private set; }

    public int VatRate { get; private set; }

    public decimal GrossAmount { get; private set; }

    public decimal NetAmount { get; private set; }

    public decimal VatAmount { get; private set; }

    public decimal DeductibleVatAmount { get; private set; }
}
