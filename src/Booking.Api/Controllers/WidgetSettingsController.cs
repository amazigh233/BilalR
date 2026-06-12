using Booking.Api.Contracts.WidgetSettings;
using Booking.Api.WidgetAssets;
using Booking.Application.WidgetSettings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Booking.Api.Controllers;

[Route("api/restaurants/{restaurantId:guid}/widget-origins")]
public sealed class WidgetSettingsController(
    GetWidgetOriginsUseCase getWidgetOriginsUseCase,
    SetWidgetOriginsUseCase setWidgetOriginsUseCase,
    GetWidgetBrandingUseCase getWidgetBrandingUseCase,
    SetWidgetBrandingUseCase setWidgetBrandingUseCase,
    IWidgetLogoStorage widgetLogoStorage) : ApiControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(WidgetOriginsApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WidgetOriginsApiResponse>> Get(
        Guid restaurantId,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await getWidgetOriginsUseCase.ExecuteAsync(
                restaurantId,
                cancellationToken);

            return Ok(ToApiResponse(response));
        }
        catch (Exception exception) when (exception is ArgumentException or KeyNotFoundException)
        {
            return HandleKnownException(exception);
        }
    }

    [HttpGet("/api/admin/restaurant/widget-origins")]
    [Authorize(Policy = "RestaurantOwner")]
    [ProducesResponseType(typeof(WidgetOriginsApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<WidgetOriginsApiResponse>> GetCurrent(
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentRestaurantId(out var restaurantId))
        {
            return Forbid();
        }

        try
        {
            var response = await getWidgetOriginsUseCase.ExecuteAsync(
                restaurantId,
                cancellationToken);

            return Ok(ToApiResponse(response));
        }
        catch (Exception exception) when (exception is ArgumentException or KeyNotFoundException)
        {
            return HandleKnownException(exception);
        }
    }

    [HttpPut("/api/admin/restaurant/widget-origins")]
    [Authorize(Policy = "RestaurantOwner")]
    [ProducesResponseType(typeof(WidgetOriginsApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<WidgetOriginsApiResponse>> SetCurrent(
        [FromBody] SetWidgetOriginsApiRequest? request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentRestaurantId(out var restaurantId))
        {
            return Forbid();
        }

        if (request?.Origins is null)
        {
            return BadRequest(ToError("Origins are required."));
        }

        try
        {
            var response = await setWidgetOriginsUseCase.ExecuteAsync(
                new SetWidgetOriginsRequest(restaurantId, request.Origins),
                cancellationToken);

            return Ok(ToApiResponse(response));
        }
        catch (Exception exception) when (exception is ArgumentException or KeyNotFoundException)
        {
            return HandleKnownException(exception);
        }
    }

    [HttpGet("/api/restaurants/{restaurantId:guid}/widget-branding")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(WidgetBrandingApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WidgetBrandingApiResponse>> GetBranding(
        Guid restaurantId,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await getWidgetBrandingUseCase.ExecuteAsync(
                restaurantId,
                cancellationToken);

            return Ok(ToApiResponse(response));
        }
        catch (Exception exception) when (exception is ArgumentException or KeyNotFoundException)
        {
            return HandleKnownException(exception);
        }
    }

    [HttpGet("/api/admin/restaurant/widget-branding")]
    [Authorize(Policy = "RestaurantOwner")]
    [ProducesResponseType(typeof(WidgetBrandingApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<WidgetBrandingApiResponse>> GetCurrentBranding(
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentRestaurantId(out var restaurantId))
        {
            return Forbid();
        }

        try
        {
            var response = await getWidgetBrandingUseCase.ExecuteAsync(
                restaurantId,
                cancellationToken);

            return Ok(ToApiResponse(response));
        }
        catch (Exception exception) when (exception is ArgumentException or KeyNotFoundException)
        {
            return HandleKnownException(exception);
        }
    }

    [HttpPut("/api/admin/restaurant/widget-branding")]
    [Authorize(Policy = "RestaurantOwner")]
    [ProducesResponseType(typeof(WidgetBrandingApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<WidgetBrandingApiResponse>> SetCurrentBranding(
        [FromBody] SetWidgetBrandingApiRequest? request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentRestaurantId(out var restaurantId))
        {
            return Forbid();
        }

        if (request is null)
        {
            return BadRequest(ToError("Request body is required."));
        }

        try
        {
            var current = await getWidgetBrandingUseCase.ExecuteAsync(
                restaurantId,
                cancellationToken);
            var response = await setWidgetBrandingUseCase.ExecuteAsync(
                new SetWidgetBrandingRequest(
                    restaurantId,
                    request.PrimaryColor,
                    request.AccentColor,
                    request.WelcomeText,
                    request.LogoUrl),
                cancellationToken);

            if (widgetLogoStorage.IsManagedUrl(current.LogoUrl) &&
                !string.Equals(current.LogoUrl, response.LogoUrl, StringComparison.Ordinal))
            {
                await widgetLogoStorage.DeleteAsync(restaurantId, cancellationToken);
            }

            return Ok(ToApiResponse(response));
        }
        catch (Exception exception) when (exception is ArgumentException or KeyNotFoundException)
        {
            return HandleKnownException(exception);
        }
    }

    [HttpPost("/api/admin/restaurant/widget-logo")]
    [Authorize(Policy = "RestaurantOwner")]
    [RequestSizeLimit(2_200_000)]
    [RequestFormLimits(MultipartBodyLengthLimit = 2_200_000)]
    [ProducesResponseType(typeof(WidgetBrandingApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<WidgetBrandingApiResponse>> UploadCurrentLogo(
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentRestaurantId(out var restaurantId))
        {
            return Forbid();
        }

        if (file is null || file.Length == 0)
        {
            return BadRequest(ToError("Kies een afbeelding om te uploaden."));
        }

        if (file.Length > IWidgetLogoStorage.MaximumFileSize)
        {
            return BadRequest(ToError("Het logo mag maximaal 2 MB groot zijn."));
        }

        try
        {
            var current = await getWidgetBrandingUseCase.ExecuteAsync(
                restaurantId,
                cancellationToken);
            await using var content = file.OpenReadStream();
            var logoUrl = await widgetLogoStorage.SaveAsync(
                restaurantId,
                content,
                cancellationToken);
            var response = await setWidgetBrandingUseCase.ExecuteAsync(
                new SetWidgetBrandingRequest(
                    restaurantId,
                    current.PrimaryColor,
                    current.AccentColor,
                    current.WelcomeText,
                    logoUrl),
                cancellationToken);

            return Ok(ToApiResponse(response));
        }
        catch (Exception exception) when (exception is ArgumentException or KeyNotFoundException)
        {
            return HandleKnownException(exception);
        }
    }

    [HttpDelete("/api/admin/restaurant/widget-logo")]
    [Authorize(Policy = "RestaurantOwner")]
    [ProducesResponseType(typeof(WidgetBrandingApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<WidgetBrandingApiResponse>> DeleteCurrentLogo(
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentRestaurantId(out var restaurantId))
        {
            return Forbid();
        }

        try
        {
            var current = await getWidgetBrandingUseCase.ExecuteAsync(
                restaurantId,
                cancellationToken);
            var response = await setWidgetBrandingUseCase.ExecuteAsync(
                new SetWidgetBrandingRequest(
                    restaurantId,
                    current.PrimaryColor,
                    current.AccentColor,
                    current.WelcomeText,
                    null),
                cancellationToken);
            await widgetLogoStorage.DeleteAsync(restaurantId, cancellationToken);

            return Ok(ToApiResponse(response));
        }
        catch (Exception exception) when (exception is ArgumentException or KeyNotFoundException)
        {
            return HandleKnownException(exception);
        }
    }

    [HttpGet("/api/restaurants/{restaurantId:guid}/widget-logo")]
    [AllowAnonymous]
    [EnableRateLimiting("public")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLogo(
        Guid restaurantId,
        CancellationToken cancellationToken)
    {
        try
        {
            var asset = await widgetLogoStorage.GetAsync(restaurantId, cancellationToken);
            Response.Headers.CacheControl = "public,max-age=31536000,immutable";
            Response.Headers.LastModified = asset.LastModifiedUtc.ToString("R");
            Response.Headers.XContentTypeOptions = "nosniff";

            return File(asset.Content, asset.ContentType, enableRangeProcessing: true);
        }
        catch (FileNotFoundException)
        {
            return NotFound(ToError("Restaurantlogo was not found."));
        }
    }

    private static WidgetOriginsApiResponse ToApiResponse(WidgetOriginsResponse response)
    {
        return new WidgetOriginsApiResponse(response.RestaurantId, response.Origins);
    }

    private static WidgetBrandingApiResponse ToApiResponse(WidgetBrandingResponse response)
    {
        return new WidgetBrandingApiResponse(
            response.RestaurantId,
            response.PrimaryColor,
            response.AccentColor,
            response.WelcomeText,
            response.LogoUrl);
    }
}
