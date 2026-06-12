using Booking.Api.Contracts.Delivery;
using Booking.Api.Delivery;
using Booking.Domain.Delivery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Booking.Api.Controllers;

[Route("api/delivery/thuisbezorgd")]
public sealed class ThuisbezorgdIngressController(
    DeliveryIngressService deliveryIngressService) : ApiControllerBase
{
    private const string JetConnectSecretHeader = "X-JET-Connect-Secret";
    private const string TConnectSecretHeader = "X-Zambiq-Connector-Secret";

    [HttpPost("jet-connect/orders")]
    [AllowAnonymous]
    [EnableRateLimiting("public")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public Task<IActionResult> ReceiveJetConnectOrder(
        [FromBody] DeliveryWebhookApiRequest? request,
        CancellationToken cancellationToken)
    {
        return Receive(request, JetConnectSecretHeader, cancellationToken);
    }

    [HttpPost("t-connect/orders")]
    [AllowAnonymous]
    [EnableRateLimiting("public")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public Task<IActionResult> ReceiveTConnectOrder(
        [FromBody] DeliveryWebhookApiRequest? request,
        CancellationToken cancellationToken)
    {
        return Receive(request, TConnectSecretHeader, cancellationToken);
    }

    private async Task<IActionResult> Receive(
        DeliveryWebhookApiRequest? request,
        string secretHeader,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(ToError("Request body is required."));
        }

        try
        {
            var result = await deliveryIngressService.ReceiveAsync(
                DeliveryProvider.Thuisbezorgd,
                Request.Headers[secretHeader].ToString(),
                request,
                cancellationToken);

            return result == DeliveryIngressResult.Accepted
                ? Accepted()
                : Unauthorized(ToError("Invalid or disabled connector secret."));
        }
        catch (Exception exception) when (exception is ArgumentException or ArgumentOutOfRangeException)
        {
            return BadRequest(ToError(exception.Message));
        }
    }
}
