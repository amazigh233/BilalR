using Booking.Api.Contracts.Delivery;
using Booking.Application.Delivery;
using Booking.Domain.Delivery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Booking.Api.Controllers;

[Route("api/delivery/webhook")]
public sealed class DeliveryWebhookController(
    ResolveDeliveryIntegrationUseCase resolveDeliveryIntegrationUseCase,
    IngestDeliveryOrderUseCase ingestDeliveryOrderUseCase) : ApiControllerBase
{
    private const string SecretHeaderName = "X-Webhook-Secret";

    [HttpPost("{provider}")]
    [AllowAnonymous]
    [EnableRateLimiting("public")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Receive(
        string provider,
        [FromBody] DeliveryWebhookApiRequest? request,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<DeliveryProvider>(provider, ignoreCase: true, out var deliveryProvider))
        {
            return BadRequest(ToError($"Unknown delivery provider '{provider}'."));
        }

        if (request is null)
        {
            return BadRequest(ToError("Request body is required."));
        }

        if (string.IsNullOrWhiteSpace(request.ExternalOrderId) ||
            string.IsNullOrWhiteSpace(request.CustomerName))
        {
            return BadRequest(ToError("externalOrderId and customerName are required."));
        }

        var secret = Request.Headers[SecretHeaderName].ToString();
        var restaurantId = await resolveDeliveryIntegrationUseCase.ExecuteAsync(
            new ResolveDeliveryIntegrationRequest(deliveryProvider, secret),
            cancellationToken);

        if (restaurantId is null)
        {
            return Unauthorized(ToError("Invalid or disabled webhook secret."));
        }

        var items = (request.Items ?? [])
            .Select(item => new IngestDeliveryOrderLine(item.Name, item.Quantity, item.UnitPrice))
            .ToList();

        try
        {
            await ingestDeliveryOrderUseCase.ExecuteAsync(
                new IngestDeliveryOrderRequest(
                    restaurantId.Value,
                    deliveryProvider,
                    request.ExternalOrderId,
                    request.CustomerName,
                    request.CustomerPhone,
                    request.DeliveryAddress,
                    request.Note,
                    request.Status,
                    request.TotalAmount,
                    request.Currency,
                    request.PlacedAtUtc,
                    items),
                cancellationToken);

            return Accepted();
        }
        catch (Exception exception) when (exception is ArgumentException or ArgumentOutOfRangeException)
        {
            return BadRequest(ToError(exception.Message));
        }
    }
}
