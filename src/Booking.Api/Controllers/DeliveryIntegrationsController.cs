using Booking.Api.Contracts.Delivery;
using Booking.Application.Delivery;
using Booking.Domain.Delivery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Booking.Api.Controllers;

[Route("api/admin/restaurant/delivery-integrations")]
public sealed class DeliveryIntegrationsController(
    GetDeliveryIntegrationsUseCase getDeliveryIntegrationsUseCase,
    ConnectDeliveryProviderUseCase connectDeliveryProviderUseCase,
    SetDeliveryProviderEnabledUseCase setDeliveryProviderEnabledUseCase,
    IConfiguration configuration) : ApiControllerBase
{
    [HttpGet]
    [Authorize(Policy = "RestaurantOwner")]
    [ProducesResponseType(typeof(IReadOnlyCollection<DeliveryIntegrationApiResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<DeliveryIntegrationApiResponse>>> Get(
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentRestaurantId(out var restaurantId))
        {
            return Forbid();
        }

        var integrations = await getDeliveryIntegrationsUseCase.ExecuteAsync(
            new GetDeliveryIntegrationsRequest(restaurantId),
            cancellationToken);

        return Ok(integrations.Select(ToApiResponse).ToList());
    }

    [HttpPost("{provider}/connect")]
    [Authorize(Policy = "RestaurantOwner")]
    [ProducesResponseType(typeof(ConnectDeliveryApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ConnectDeliveryApiResponse>> Connect(
        string provider,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<DeliveryProvider>(provider, ignoreCase: true, out var deliveryProvider))
        {
            return BadRequest(ToError($"Unknown delivery provider '{provider}'."));
        }

        if (!TryGetCurrentRestaurantId(out var restaurantId))
        {
            return Forbid();
        }

        var result = await connectDeliveryProviderUseCase.ExecuteAsync(
            new ConnectDeliveryProviderRequest(restaurantId, deliveryProvider),
            cancellationToken);

        return Ok(new ConnectDeliveryApiResponse(
            result.Provider,
            BuildPublicUrl($"api/delivery/webhook/{deliveryProvider}"),
            result.Secret,
            deliveryProvider == DeliveryProvider.Thuisbezorgd
                ? BuildPublicUrl("api/delivery/thuisbezorgd/jet-connect/orders")
                : null,
            deliveryProvider == DeliveryProvider.Thuisbezorgd
                ? BuildPublicUrl("api/delivery/thuisbezorgd/t-connect/orders")
                : null));
    }

    [HttpPost("{provider}/disable")]
    [Authorize(Policy = "RestaurantOwner")]
    [ProducesResponseType(typeof(DeliveryIntegrationApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DeliveryIntegrationApiResponse>> Disable(
        string provider,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<DeliveryProvider>(provider, ignoreCase: true, out var deliveryProvider))
        {
            return BadRequest(ToError($"Unknown delivery provider '{provider}'."));
        }

        if (!TryGetCurrentRestaurantId(out var restaurantId))
        {
            return Forbid();
        }

        try
        {
            var response = await setDeliveryProviderEnabledUseCase.ExecuteAsync(
                new SetDeliveryProviderEnabledRequest(restaurantId, deliveryProvider, Enabled: false),
                cancellationToken);

            return Ok(ToApiResponse(response));
        }
        catch (Exception exception) when (exception is KeyNotFoundException)
        {
            return HandleKnownException(exception);
        }
    }

    private string BuildPublicUrl(string path)
    {
        var configuredBaseUrl = configuration["Delivery:PublicBaseUrl"];
        var baseUrl = string.IsNullOrWhiteSpace(configuredBaseUrl)
            ? $"{Request.Scheme}://{Request.Host}"
            : configuredBaseUrl.TrimEnd('/');

        return $"{baseUrl}/{path.TrimStart('/')}";
    }

    private static DeliveryIntegrationApiResponse ToApiResponse(DeliveryIntegrationResponse response)
    {
        return new DeliveryIntegrationApiResponse(
            response.Provider,
            response.Connected,
            response.Enabled,
            response.CreatedAtUtc,
            response.LastRotatedAtUtc);
    }
}
