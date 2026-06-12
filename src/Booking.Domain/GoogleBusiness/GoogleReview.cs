namespace Booking.Domain.GoogleBusiness;

public sealed class GoogleReview
{
    private GoogleReview()
    {
        ReviewName = string.Empty;
    }

    public GoogleReview(
        Guid restaurantId,
        string reviewName,
        string? reviewerDisplayName,
        int starRating,
        string? comment,
        DateTime createTime,
        DateTime updateTime,
        string? replyComment,
        DateTime? replyUpdateTime,
        DateTime syncedAtUtc)
    {
        Id = Guid.NewGuid();
        RestaurantId = restaurantId;
        ReviewName = reviewName;
        ReviewerDisplayName = reviewerDisplayName;
        StarRating = starRating;
        Comment = comment;
        CreateTime = createTime;
        UpdateTime = updateTime;
        ReplyComment = replyComment;
        ReplyUpdateTime = replyUpdateTime;
        SyncedAtUtc = syncedAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid RestaurantId { get; private set; }
    public string ReviewName { get; private set; }
    public string? ReviewerDisplayName { get; private set; }
    public int StarRating { get; private set; }
    public string? Comment { get; private set; }
    public DateTime CreateTime { get; private set; }
    public DateTime UpdateTime { get; private set; }
    public string? ReplyComment { get; private set; }
    public DateTime? ReplyUpdateTime { get; private set; }
    public bool IsRead { get; private set; }
    public DateTime SyncedAtUtc { get; private set; }

    public void UpdateFromGbp(
        string? reviewerDisplayName,
        int starRating,
        string? comment,
        DateTime updateTime,
        string? replyComment,
        DateTime? replyUpdateTime,
        DateTime syncedAtUtc)
    {
        ReviewerDisplayName = reviewerDisplayName;
        StarRating = starRating;
        Comment = comment;
        UpdateTime = updateTime;
        ReplyComment = replyComment;
        ReplyUpdateTime = replyUpdateTime;
        SyncedAtUtc = syncedAtUtc;
    }

    public void SetReply(string comment, DateTime updatedAt)
    {
        ReplyComment = comment;
        ReplyUpdateTime = updatedAt;
    }

    public void DeleteReply()
    {
        ReplyComment = null;
        ReplyUpdateTime = null;
    }

    public void MarkRead()
    {
        IsRead = true;
    }
}
