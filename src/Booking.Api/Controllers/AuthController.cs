using System.Security.Claims;
using Booking.Api.Authentication;
using Booking.Api.Contracts.Auth;
using Booking.Application.Restaurants;
using Booking.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Booking.Api.Controllers;

[Route("api/auth")]
public sealed class AuthController(
    UserManager<ApplicationUser> userManager,
    GetRestaurantUseCase getRestaurantUseCase,
    JwtTokenService jwtTokenService) : ApiControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginApiResponse>> Login(
        [FromBody] LoginApiRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(ToError("Request body is required."));
        }

        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(ToError("Email and password are required."));
        }

        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
        {
            return Unauthorized(ToError("Email or password is incorrect."));
        }

        if (IsDisabled(user))
        {
            return Unauthorized(ToError("Account is uitgeschakeld."));
        }

        try
        {
            var restaurant = await getRestaurantUseCase.ExecuteAsync(
                user.RestaurantId,
                cancellationToken);

            var token = await jwtTokenService.CreateTokenAsync(user, cancellationToken);

            return Ok(new LoginApiResponse(
                token.AccessToken,
                token.ExpiresAtUtc,
                new AuthenticatedUserApiResponse(
                    user.Id,
                    user.Email ?? string.Empty,
                    user.DisplayName,
                    user.RestaurantId,
                    restaurant.Name,
                    token.Roles)));
        }
        catch (Exception exception) when (exception is ArgumentException or KeyNotFoundException)
        {
            return HandleKnownException(exception);
        }
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(AuthenticatedUserApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthenticatedUserApiResponse>> Me(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(ToError("Authentication is required."));
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return Unauthorized(ToError("Authentication is required."));
        }

        try
        {
            var restaurant = await getRestaurantUseCase.ExecuteAsync(
                user.RestaurantId,
                cancellationToken);

            var roles = await userManager.GetRolesAsync(user);

            return Ok(new AuthenticatedUserApiResponse(
                user.Id,
                user.Email ?? string.Empty,
                user.DisplayName,
                user.RestaurantId,
                restaurant.Name,
                roles.ToList()));
        }
        catch (Exception exception) when (exception is ArgumentException or KeyNotFoundException)
        {
            return HandleKnownException(exception);
        }
    }

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Logout()
    {
        return NoContent();
    }

    private static bool IsDisabled(ApplicationUser user)
    {
        return user.LockoutEnd.HasValue &&
            user.LockoutEnd.Value > DateTimeOffset.UtcNow;
    }
}
