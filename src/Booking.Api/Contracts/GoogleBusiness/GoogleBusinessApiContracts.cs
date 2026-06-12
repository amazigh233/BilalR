using Booking.Domain.GoogleBusiness;

namespace Booking.Api.Contracts.GoogleBusiness;

public sealed record GbpConnectApiResponse(string AuthorizationUrl);

public sealed record GbpCompleteApiRequest(string State, string Code);

public sealed record GbpLocationSummaryApiResponse(
    string AccountName,
    string LocationName,
    string Title,
    string? StoreCode);

public sealed record GbpSelectLocationApiRequest(string AccountName, string LocationName);

public sealed record GbpStatusApiResponse(
    GoogleBusinessConnectionStatus Status,
    string? GbpLocationName,
    string? GbpAccountName,
    DateTime? LastReviewSyncAtUtc,
    DateTime? LastHoursSyncAtUtc,
    string? LastSyncError,
    bool IsConfigured);

public sealed record GbpReviewApiResponse(
    Guid Id,
    string ReviewName,
    string? ReviewerDisplayName,
    int StarRating,
    string? Comment,
    DateTime CreateTime,
    DateTime UpdateTime,
    string? ReplyComment,
    DateTime? ReplyUpdateTime,
    bool IsRead);

public sealed record GbpReplyApiRequest(string Comment);
