using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Booking.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace Booking.Api.Authentication;

public sealed class JwtTokenService(
    JwtAuthenticationOptions options,
    UserManager<ApplicationUser> userManager)
{
    public async Task<(string AccessToken, DateTime ExpiresAtUtc, IReadOnlyCollection<string> Roles)> CreateTokenAsync(
        ApplicationUser user,
        CancellationToken cancellationToken = default)
    {
        var roles = await userManager.GetRolesAsync(user);
        var expiresAtUtc = DateTime.UtcNow.Add(options.TokenLifetime);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Name, string.IsNullOrWhiteSpace(user.DisplayName)
                ? user.Email ?? user.UserName ?? user.Id.ToString()
                : user.DisplayName),
            new(BookingClaimTypes.RestaurantId, user.RestaurantId.ToString())
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            options.Issuer,
            options.Audience,
            claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        cancellationToken.ThrowIfCancellationRequested();

        return (
            new JwtSecurityTokenHandler().WriteToken(token),
            expiresAtUtc,
            roles.ToList());
    }
}
