using Booking.Application.Abstractions;
using Booking.Application.Notifications;
using Booking.Domain.Customers;
using Booking.Domain.Notifications;
using Booking.Domain.Reservations;
using Booking.Domain.Restaurants;

namespace Booking.Application.Reservations;

public sealed class CreateReservationUseCase(
    IRestaurantRepository restaurantRepository,
    IReservationRepository reservationRepository,
    IAvailabilityService availabilityService,
    IEmailSender emailSender,
    INotificationLogRepository notificationLogRepository,
    TimeProvider? timeProvider = null)
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public async Task<ReservationResponse> ExecuteAsync(
        CreateReservationRequest request,
        CancellationToken cancellationToken = default)
    {
        var restaurant = await restaurantRepository.GetByIdAsync(request.RestaurantId, cancellationToken);
        if (restaurant is null)
        {
            throw new KeyNotFoundException("Restaurant was not found.");
        }

        if (request.ReservationDateTime <= _timeProvider.GetLocalNow().DateTime)
        {
            throw new InvalidOperationException("Reservering moet in de toekomst liggen.");
        }

        var availability = await availabilityService.CheckAsync(
            request.RestaurantId,
            request.ReservationDateTime,
            request.PartySize,
            cancellationToken);

        if (!availability.IsAvailable)
        {
            throw new InvalidOperationException(availability.Reason ?? "Reservation is not available.");
        }

        var customer = new Customer(
            request.Customer.Name,
            request.Customer.Email,
            request.Customer.PhoneNumber);

        var reservation = new Reservation(
            restaurant.Id,
            customer,
            request.ReservationDateTime,
            request.PartySize,
            request.Note);

        await reservationRepository.AddAsync(reservation, cancellationToken);

        await SendNotificationsAsync(reservation, customer, restaurant, cancellationToken);

        return ReservationResponse.FromReservation(reservation);
    }

    private async Task SendNotificationsAsync(
        Reservation reservation,
        Customer customer,
        Restaurant restaurant,
        CancellationToken cancellationToken)
    {
        var when = reservation.ReservationDateTime.ToString("dd-MM-yyyy HH:mm");

        // Confirmation to the customer.
        await NotifyAsync(
            reservation,
            restaurant,
            customer.Email,
            $"Bevestiging van je reservering bij {restaurant.Name}",
            $"Beste {customer.Name},\n\n" +
            $"We hebben je reservering ontvangen bij {restaurant.Name}.\n\n" +
            $"Datum en tijd: {when}\n" +
            $"Aantal personen: {reservation.PartySize}\n" +
            (string.IsNullOrWhiteSpace(reservation.Note) ? string.Empty : $"Opmerking: {reservation.Note}\n") +
            "\nJe ontvangt bericht zodra de reservering is bevestigd.\n\n" +
            "Met vriendelijke groet,\n" +
            restaurant.Name,
            cancellationToken);

        // Notification to the restaurant (only when an e-mail address is known).
        if (!string.IsNullOrWhiteSpace(restaurant.Email))
        {
            await NotifyAsync(
                reservation,
                restaurant,
                restaurant.Email,
                $"Nieuwe reservering: {customer.Name} op {when}",
                $"Er is een nieuwe reservering binnengekomen.\n\n" +
                $"Naam: {customer.Name}\n" +
                $"E-mail: {customer.Email}\n" +
                (string.IsNullOrWhiteSpace(customer.PhoneNumber) ? string.Empty : $"Telefoon: {customer.PhoneNumber}\n") +
                $"Datum en tijd: {when}\n" +
                $"Aantal personen: {reservation.PartySize}\n" +
                (string.IsNullOrWhiteSpace(reservation.Note) ? string.Empty : $"Opmerking: {reservation.Note}\n"),
                cancellationToken);
        }
    }

    private async Task NotifyAsync(
        Reservation reservation,
        Restaurant restaurant,
        string recipientEmail,
        string subject,
        string body,
        CancellationToken cancellationToken)
    {
        // E-mail problems must never fail the reservation; outcome is recorded in NotificationLog.
        NotificationLog? log = null;
        try
        {
            log = new NotificationLog(
                reservation.Id,
                restaurant.Id,
                recipientEmail,
                subject,
                _timeProvider.GetUtcNow().UtcDateTime);

            await notificationLogRepository.AddAsync(log, cancellationToken);

            await emailSender.SendAsync(new EmailMessage(recipientEmail, subject, body), cancellationToken);

            log.MarkSent(_timeProvider.GetUtcNow().UtcDateTime);
            await notificationLogRepository.UpdateAsync(log, cancellationToken);
        }
        catch (Exception exception)
        {
            if (log is not null)
            {
                try
                {
                    log.MarkFailed(exception.Message);
                    await notificationLogRepository.UpdateAsync(log, cancellationToken);
                }
                catch
                {
                    // Best-effort: never let notification bookkeeping break the reservation.
                }
            }
        }
    }
}
