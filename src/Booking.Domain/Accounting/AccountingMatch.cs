namespace Booking.Domain.Accounting;

public sealed class AccountingMatch
{
    private AccountingMatch()
    {
    }

    public AccountingMatch(
        Guid restaurantId,
        Guid bankSourceTransactionId,
        Guid matchedSourceTransactionId,
        DateTime createdAtUtc)
    {
        Id = Guid.NewGuid();
        RestaurantId = restaurantId;
        BankSourceTransactionId = bankSourceTransactionId;
        MatchedSourceTransactionId = matchedSourceTransactionId;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid RestaurantId { get; private set; }
    public Guid BankSourceTransactionId { get; private set; }
    public Guid MatchedSourceTransactionId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
}
