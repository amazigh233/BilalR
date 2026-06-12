namespace Booking.Domain.Accounting;

public enum AccountingEntryType
{
    Revenue,
    Expense,
    Transfer
}

public enum AccountingEntryStatus
{
    Draft,
    Confirmed,
    Corrected
}

public enum AccountingSourceKind
{
    Manual,
    BankCsv,
    PosCsv,
    GoCardless,
    Mollie,
    Delivery
}

public enum AccountingSourceStatus
{
    Pending,
    Reviewed,
    Ignored,
    Matched
}

public enum AccountingImportKind
{
    Bank,
    Pos
}

public enum AccountingConnectionProvider
{
    GoCardless,
    Mollie
}

public enum AccountingConnectionStatus
{
    Pending,
    Connected,
    ReconnectRequired,
    Disabled
}
