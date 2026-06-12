using Booking.Application.Accounting;
using Booking.Application.Tests.Fakes;
using Booking.Domain.Accounting;
using Booking.Domain.Delivery;

namespace Booking.Application.Tests.Accounting;

public sealed class AccountingUseCasesTests
{
    private readonly Guid _restaurantId = Guid.NewGuid();

    [Fact]
    public async Task Summary_UsesConfirmedEntries_AndVatSplits()
    {
        var repository = new FakeAccountingRepository();
        var useCases = new AccountingUseCases(repository, new FakeDeliveryOrderRepository());
        var revenue = await useCases.CreateDraftAsync(Request(AccountingEntryType.Revenue, 109m, 9, 0m));
        await useCases.ConfirmAsync(_restaurantId, revenue.Id);
        var expense = await useCases.CreateDraftAsync(Request(AccountingEntryType.Expense, 121m, 21, 21m));
        await useCases.ConfirmAsync(_restaurantId, expense.Id);

        var summary = await useCases.GetSummaryAsync(_restaurantId, DateOnly.FromDateTime(DateTime.Today).AddDays(-1), DateOnly.FromDateTime(DateTime.Today).AddDays(1));

        Assert.Equal(109m, summary.Revenue);
        Assert.Equal(121m, summary.Expenses);
        Assert.Equal(-12m, summary.Result);
        Assert.Equal(-12m, summary.VatBalance);
    }

    [Fact]
    public async Task Correction_AddsReversalAndReplacement()
    {
        var repository = new FakeAccountingRepository();
        var useCases = new AccountingUseCases(repository, new FakeDeliveryOrderRepository());
        var original = await useCases.CreateDraftAsync(Request(AccountingEntryType.Revenue, 109m, 9, 0m));
        await useCases.ConfirmAsync(_restaurantId, original.Id);

        await useCases.CorrectAsync(new CorrectAccountingEntryRequest(
            _restaurantId, original.Id, AccountingEntryType.Revenue, DateOnly.FromDateTime(DateTime.Today), "Nieuwe omzet",
            [new AccountingSplitRequest(null, 9, 218m, 0m)]));

        Assert.Equal(3, repository.Entries.Count);
        var summary = await useCases.GetSummaryAsync(_restaurantId, DateOnly.FromDateTime(DateTime.Today), DateOnly.FromDateTime(DateTime.Today));
        Assert.Equal(218m, summary.Revenue);
    }

    [Fact]
    public async Task DeliverySync_IsIdempotent_AndGroupsPerDay()
    {
        var repository = new FakeAccountingRepository();
        var delivery = new FakeDeliveryOrderRepository();
        delivery.Orders.Add(new DeliveryOrder(
            _restaurantId, DeliveryProvider.Thuisbezorgd, "1", "Klant", null, null, null, "Confirmed", 25m, "EUR",
            DateTime.UtcNow, DateTime.UtcNow, [new DeliveryOrderLine("Eten", 1, 25m)]));
        delivery.Orders.Add(new DeliveryOrder(
            _restaurantId, DeliveryProvider.Thuisbezorgd, "2", "Klant", null, null, null, "Confirmed", 15m, "EUR",
            DateTime.UtcNow, DateTime.UtcNow, [new DeliveryOrderLine("Eten", 1, 15m)]));
        var useCases = new AccountingUseCases(repository, delivery);

        Assert.Equal(1, await useCases.SyncDeliverySalesAsync(_restaurantId));
        Assert.Equal(0, await useCases.SyncDeliverySalesAsync(_restaurantId));
        Assert.Equal(40m, repository.Sources.Single().Amount);
    }

    [Fact]
    public async Task MatchingBankPayout_DoesNotConsumeSalesSource()
    {
        var repository = new FakeAccountingRepository();
        var bank = Source(AccountingSourceKind.BankCsv, "bank", 100m);
        var sale = Source(AccountingSourceKind.Mollie, "sale", 100m);
        repository.Sources.AddRange([bank, sale]);
        var useCases = new AccountingUseCases(repository, new FakeDeliveryOrderRepository());

        await useCases.MatchSourcesAsync(_restaurantId, bank.Id, sale.Id);

        Assert.Equal(AccountingSourceStatus.Matched, bank.Status);
        Assert.Equal(AccountingSourceStatus.Pending, sale.Status);
        Assert.Single(repository.Matches);
    }

    private SaveAccountingEntryRequest Request(AccountingEntryType type, decimal gross, int vat, decimal deductible) =>
        new(_restaurantId, type, DateOnly.FromDateTime(DateTime.Today), "Test", [new AccountingSplitRequest(null, vat, gross, deductible)]);

    private AccountingSourceTransaction Source(AccountingSourceKind kind, string externalId, decimal amount) =>
        new(_restaurantId, kind, externalId, $"fingerprint-{externalId}", DateOnly.FromDateTime(DateTime.Today), externalId, amount, "EUR", "{}", DateTime.UtcNow);
}
