using Booking.Application.Abstractions;
using Booking.Domain.Reservations;

namespace Booking.Application.Analytics;

public sealed class GetReservationAnalyticsUseCase(
    IRestaurantRepository restaurantRepository,
    IReservationRepository reservationRepository)
{
    public async Task<ReservationAnalyticsResponse> ExecuteAsync(
        ReservationAnalyticsRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.RestaurantId == Guid.Empty)
        {
            throw new ArgumentException("Restaurant id is required.", nameof(request));
        }

        if (request.ToDate < request.FromDate)
        {
            throw new ArgumentException("To date cannot be before from date.", nameof(request));
        }

        var restaurant = await restaurantRepository.GetByIdAsync(request.RestaurantId, cancellationToken);
        if (restaurant is null)
        {
            throw new KeyNotFoundException("Restaurant was not found.");
        }

        var fromInclusive = request.FromDate.ToDateTime(TimeOnly.MinValue);
        var toExclusive = request.ToDate.AddDays(1).ToDateTime(TimeOnly.MinValue);

        var reservations = await reservationRepository.GetByRestaurantAndDateRangeAsync(
            request.RestaurantId,
            fromInclusive,
            toExclusive,
            cancellationToken);

        var total = reservations.Count;
        var newCount = reservations.Count(reservation => reservation.Status == ReservationStatus.New);
        var confirmedCount = reservations.Count(reservation => reservation.Status == ReservationStatus.Confirmed);
        var cancelledCount = reservations.Count(reservation => reservation.Status == ReservationStatus.Cancelled);
        var noShowCount = reservations.Count(reservation => reservation.Status == ReservationStatus.NoShow);

        var totalGuests = reservations.Sum(reservation => reservation.PartySize);
        var noShowRate = total == 0 ? 0d : (double)noShowCount / total;
        var averagePartySize = total == 0 ? 0d : (double)totalGuests / total;

        var perDay = BuildPerDay(reservations, request.FromDate, request.ToDate);
        var perWeekday = BuildPerWeekday(reservations);
        var perHour = BuildPerHour(reservations);

        return new ReservationAnalyticsResponse(
            request.FromDate,
            request.ToDate,
            total,
            newCount,
            confirmedCount,
            cancelledCount,
            noShowCount,
            noShowRate,
            totalGuests,
            averagePartySize,
            perDay,
            perWeekday,
            perHour);
    }

    private static IReadOnlyList<DailyReservationCount> BuildPerDay(
        IReadOnlyCollection<Reservation> reservations,
        DateOnly fromDate,
        DateOnly toDate)
    {
        var countsByDate = reservations
            .GroupBy(reservation => DateOnly.FromDateTime(reservation.ReservationDateTime))
            .ToDictionary(group => group.Key, group => group.Count());

        var result = new List<DailyReservationCount>();
        for (var date = fromDate; date <= toDate; date = date.AddDays(1))
        {
            result.Add(new DailyReservationCount(date, countsByDate.GetValueOrDefault(date, 0)));
        }

        return result;
    }

    private static IReadOnlyList<WeekdayReservationCount> BuildPerWeekday(
        IReadOnlyCollection<Reservation> reservations)
    {
        var countsByWeekday = reservations
            .GroupBy(reservation => reservation.ReservationDateTime.DayOfWeek)
            .ToDictionary(group => group.Key, group => group.Count());

        // Monday-first ordering.
        var weekdays = new[]
        {
            DayOfWeek.Monday,
            DayOfWeek.Tuesday,
            DayOfWeek.Wednesday,
            DayOfWeek.Thursday,
            DayOfWeek.Friday,
            DayOfWeek.Saturday,
            DayOfWeek.Sunday
        };

        return weekdays
            .Select(weekday => new WeekdayReservationCount(weekday, countsByWeekday.GetValueOrDefault(weekday, 0)))
            .ToList();
    }

    private static IReadOnlyList<HourlyReservationCount> BuildPerHour(
        IReadOnlyCollection<Reservation> reservations)
    {
        var countsByHour = reservations
            .GroupBy(reservation => reservation.ReservationDateTime.Hour)
            .ToDictionary(group => group.Key, group => group.Count());

        return Enumerable.Range(0, 24)
            .Select(hour => new HourlyReservationCount(hour, countsByHour.GetValueOrDefault(hour, 0)))
            .ToList();
    }
}
