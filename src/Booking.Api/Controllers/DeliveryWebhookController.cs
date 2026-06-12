using Booking.Api.Contracts.Delivery;
using Booking.Api.Delivery;
using Booking.Domain.Delivery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Booking.Api.Controllers;

[Route("api/delivery/webhook")]
public sealed class DeliveryWebhookController(
    DeliveryIngressService deliveryIngressService) : ApiControllerBase
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

        var secret = Request.Headers[SecretHeaderName].ToString();

        try
        {
            var result = await deliveryIngressService.ReceiveAsync(
                deliveryProvider,
                secret,
                request,
                cancellationToken);

            return result == DeliveryIngressResult.Accepted
                ? Accepted()
                : Unauthorized(ToError("Invalid or disabled webhook secret."));
        }
        catch (Exception exception) when (exception is ArgumentException or ArgumentOutOfRangeException)
        {
            return BadRequest(ToError(exception.Message));
        }
    }
}
