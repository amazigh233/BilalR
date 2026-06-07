using Booking.Api.Contracts.Analytics;
using Booking.Application.Analytics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Booking.Api.Controllers;

[Route("api/admin/restaurant/analytics")]
public sealed class AnalyticsController(
    GetReservationAnalyticsUseCase getReservationAnalyticsUseCase) : ApiControllerBase
{
    [HttpGet]
    [Authorize(Policy = "RestaurantOwner")]
    [ProducesResponseType(typeof(ReservationAnalyticsApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReservationAnalyticsApiResponse>> Get(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentRestaurantId(out var restaurantId))
        {
            return Forbid();
        }

        var toDate = to ?? DateOnly.FromDateTime(DateTime.Now);
        var fromDate = from ?? toDate.AddDays(-29);

        try
        {
            var response = await getReservationAnalyticsUseCase.ExecuteAsync(
                new ReservationAnalyticsRequest(restaurantId, fromDate, toDate),
                cancellationToken);

            return Ok(ToApiResponse(response));
        }
        catch (Exception exception) when (exception is ArgumentException or KeyNotFoundException)
        {
            return HandleKnownException(exception);
        }
    }

    private static ReservationAnalyticsApiResponse ToApiResponse(ReservationAnalyticsResponse response)
    {
        return new ReservationAnalyticsApiResponse(
            response.FromDate,
            response.ToDate,
            response.TotalReservations,
            response.NewCount,
            response.ConfirmedCount,
            response.CancelledCount,
            response.NoShowCount,
            response.NoShowRate,
            response.TotalGuests,
            response.AveragePartySize,
            response.PerDay
                .Select(item => new DailyReservationCountDto(item.Date, item.Count))
                .ToList(),
            response.PerWeekday
                .Select(item => new WeekdayReservationCountDto(item.DayOfWeek, item.Count))
                .ToList(),
            response.PerHour
                .Select(item => new HourlyReservationCountDto(item.Hour, item.Count))
                .ToList());
    }
}
