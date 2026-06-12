using Booking.Api.Contracts.GoogleBusiness;
using Booking.Api.GoogleBusiness;
using Booking.Application.Abstractions;
using Booking.Domain.GoogleBusiness;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Booking.Api.Controllers;

[Route("api/admin/restaurant/google-business")]
public sealed class GoogleBusinessController(
    GoogleBusinessService googleBusinessService,
    GoogleBusinessHoursSyncService hoursSyncService,
    IGoogleBusinessRepository repository) : ApiControllerBase
{
    [HttpGet("status")]
    [Authorize(Policy = "RestaurantOwner")]
    [ProducesResponseType(typeof(GbpStatusApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<GbpStatusApiResponse>> GetStatus(CancellationToken cancellationToken)
    {
        if (!IsEnabled()) return NotFound();
        if (!TryGetCurrentRestaurantId(out var restaurantId)) return Forbid();

        var connection = await repository.GetConnectionAsync(restaurantId, cancellationToken);
        return Ok(new GbpStatusApiResponse(
            connection?.Status ?? GoogleBusinessConnectionStatus.Disabled,
            connection?.GbpLocationName,
            connection?.GbpAccountName,
            connection?.LastReviewSyncAtUtc,
            connection?.LastHoursSyncAtUtc,
            connection?.LastSyncError,
            googleBusinessService.IsConfigured));
    }

    [HttpPost("connect")]
    [Authorize(Policy = "RestaurantOwner")]
    [ProducesResponseType(typeof(GbpConnectApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<GbpConnectApiResponse>> Connect(CancellationToken cancellationToken)
    {
        if (!IsEnabled()) return NotFound();
        if (!TryGetCurrentRestaurantId(out var restaurantId)) return Forbid();

        try
        {
            var authUrl = await googleBusinessService.StartOAuthAsync(restaurantId, cancellationToken);
            return Ok(new GbpConnectApiResponse(authUrl));
        }
        catch (Exception ex)
        {
            return HandleKnownException(ex);
        }
    }

    [HttpPost("complete")]
    [Authorize(Policy = "RestaurantOwner")]
    [ProducesResponseType(typeof(IReadOnlyCollection<GbpLocationSummaryApiResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<GbpLocationSummaryApiResponse>>> Complete(
        [FromBody] GbpCompleteApiRequest? request,
        CancellationToken cancellationToken)
    {
        if (!IsEnabled()) return NotFound();
        if (request is null) return BadRequest(ToError("Ongeldig verzoek."));
        if (!TryGetCurrentRestaurantId(out _)) return Forbid();

        try
        {
            var locations = await googleBusinessService.CompleteOAuthAsync(request.State, request.Code, cancellationToken);
            return Ok(locations.Select(l => new GbpLocationSummaryApiResponse(
                l.AccountName, l.LocationName, l.Title, l.StoreCode)).ToList());
        }
        catch (Exception ex)
        {
            return HandleKnownException(ex);
        }
    }

    [HttpPost("select-location")]
    [Authorize(Policy = "RestaurantOwner")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SelectLocation(
        [FromBody] GbpSelectLocationApiRequest? request,
        CancellationToken cancellationToken)
    {
        if (!IsEnabled()) return NotFound();
        if (request is null) return BadRequest(ToError("Ongeldig verzoek."));
        if (!TryGetCurrentRestaurantId(out var restaurantId)) return Forbid();

        try
        {
            await googleBusinessService.FinalizeLocationAsync(restaurantId, request.AccountName, request.LocationName, cancellationToken);
            return NoContent();
        }
        catch (Exception ex)
        {
            return HandleKnownException(ex);
        }
    }

    [HttpPost("disconnect")]
    [Authorize(Policy = "RestaurantOwner")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Disconnect(CancellationToken cancellationToken)
    {
        if (!IsEnabled()) return NotFound();
        if (!TryGetCurrentRestaurantId(out var restaurantId)) return Forbid();

        try
        {
            await googleBusinessService.DisconnectAsync(restaurantId, cancellationToken);
            return NoContent();
        }
        catch (Exception ex)
        {
            return HandleKnownException(ex);
        }
    }

    [HttpGet("reviews")]
    [Authorize(Policy = "RestaurantUser")]
    [ProducesResponseType(typeof(IReadOnlyCollection<GbpReviewApiResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<GbpReviewApiResponse>>> GetReviews(CancellationToken cancellationToken)
    {
        if (!IsEnabled()) return NotFound();
        if (!TryGetCurrentRestaurantId(out var restaurantId)) return Forbid();

        var reviews = await repository.GetReviewsAsync(restaurantId, cancellationToken);
        return Ok(reviews.Select(ToReviewResponse).ToList());
    }

    [HttpPost("reviews/{encodedReviewName}/reply")]
    [Authorize(Policy = "RestaurantOwner")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpsertReply(
        string encodedReviewName,
        [FromBody] GbpReplyApiRequest? request,
        CancellationToken cancellationToken)
    {
        if (!IsEnabled()) return NotFound();
        if (request is null) return BadRequest(ToError("Ongeldig verzoek."));
        if (!TryGetCurrentRestaurantId(out var restaurantId)) return Forbid();

        var reviewName = Uri.UnescapeDataString(encodedReviewName);
        var connection = await repository.GetConnectionAsync(restaurantId, cancellationToken);
        if (connection is null || connection.Status != GoogleBusinessConnectionStatus.Connected)
            return BadRequest(ToError("Google Business Profile is niet verbonden."));

        var review = await repository.GetReviewByNameAsync(restaurantId, reviewName, cancellationToken);
        if (review is null) return NotFound(ToError("Beoordeling niet gevonden."));

        try
        {
            await googleBusinessService.UpsertReviewReplyAsync(connection, reviewName, request.Comment, cancellationToken);
            review.SetReply(request.Comment, DateTime.UtcNow);
            await repository.UpdateReviewAsync(review, cancellationToken);
            return NoContent();
        }
        catch (Exception ex)
        {
            return HandleKnownException(ex);
        }
    }

    [HttpDelete("reviews/{encodedReviewName}/reply")]
    [Authorize(Policy = "RestaurantOwner")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteReply(
        string encodedReviewName,
        CancellationToken cancellationToken)
    {
        if (!IsEnabled()) return NotFound();
        if (!TryGetCurrentRestaurantId(out var restaurantId)) return Forbid();

        var reviewName = Uri.UnescapeDataString(encodedReviewName);
        var connection = await repository.GetConnectionAsync(restaurantId, cancellationToken);
        if (connection is null || connection.Status != GoogleBusinessConnectionStatus.Connected)
            return BadRequest(ToError("Google Business Profile is niet verbonden."));

        var review = await repository.GetReviewByNameAsync(restaurantId, reviewName, cancellationToken);
        if (review is null) return NotFound(ToError("Beoordeling niet gevonden."));

        try
        {
            await googleBusinessService.DeleteReviewReplyAsync(connection, reviewName, cancellationToken);
            review.DeleteReply();
            await repository.UpdateReviewAsync(review, cancellationToken);
            return NoContent();
        }
        catch (Exception ex)
        {
            return HandleKnownException(ex);
        }
    }

    [HttpPost("reviews/{encodedReviewName}/read")]
    [Authorize(Policy = "RestaurantUser")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkRead(string encodedReviewName, CancellationToken cancellationToken)
    {
        if (!IsEnabled()) return NotFound();
        if (!TryGetCurrentRestaurantId(out var restaurantId)) return Forbid();

        var reviewName = Uri.UnescapeDataString(encodedReviewName);
        var review = await repository.GetReviewByNameAsync(restaurantId, reviewName, cancellationToken);
        if (review is null) return NotFound(ToError("Beoordeling niet gevonden."));

        review.MarkRead();
        await repository.UpdateReviewAsync(review, cancellationToken);
        return NoContent();
    }

    [HttpPost("sync/hours")]
    [Authorize(Policy = "RestaurantOwner")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SyncHours(CancellationToken cancellationToken)
    {
        if (!IsEnabled()) return NotFound();
        if (!TryGetCurrentRestaurantId(out var restaurantId)) return Forbid();

        var connection = await repository.GetConnectionAsync(restaurantId, cancellationToken);
        if (connection is null || connection.Status != GoogleBusinessConnectionStatus.Connected)
            return BadRequest(ToError("Google Business Profile is niet verbonden."));

        try
        {
            await hoursSyncService.SyncAsync(restaurantId, cancellationToken);
            return NoContent();
        }
        catch (Exception ex)
        {
            return HandleKnownException(ex);
        }
    }

    private bool IsEnabled() => googleBusinessService.IsEnabled;

    private static GbpReviewApiResponse ToReviewResponse(GoogleReview r) =>
        new(r.Id, r.ReviewName, r.ReviewerDisplayName, r.StarRating, r.Comment,
            r.CreateTime, r.UpdateTime, r.ReplyComment, r.ReplyUpdateTime, r.IsRead);
}
