using System.Text.RegularExpressions;

namespace Booking.BlazorApp.Security;

public static partial class WidgetSecurityPolicy
{
    private const string ConfigurationKey = "Widget:AllowedFrameAncestors";

    public static string CreateFrameAncestorsDirective(
        IConfiguration configuration,
        IEnumerable<string>? additionalAncestors = null)
    {
        var ancestors = ReadConfiguredAncestors(configuration);
        if (ancestors.Count == 0)
        {
            ancestors.Add("'self'");
        }

        if (additionalAncestors is not null)
        {
            ancestors.AddRange(additionalAncestors);
        }

        foreach (var ancestor in ancestors)
        {
            if (!IsValidAncestor(ancestor))
            {
                throw new InvalidOperationException(
                    $"Invalid value '{ancestor}' in configuration '{ConfigurationKey}'.");
            }
        }

        if (ancestors.Contains("'none'", StringComparer.Ordinal) && ancestors.Count > 1)
        {
            throw new InvalidOperationException(
                $"Configuration '{ConfigurationKey}' cannot combine 'none' with other sources.");
        }

        if (ancestors.Contains("*", StringComparer.Ordinal))
        {
            return "frame-ancestors *";
        }

        return $"frame-ancestors {string.Join(' ', ancestors.Distinct(StringComparer.Ordinal))}";
    }

    public static bool TryGetRestaurantId(PathString path, out Guid restaurantId)
    {
        const string prefix = "/embed/booking/";
        var value = path.Value;
        if (value is null || !value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            restaurantId = Guid.Empty;
            return false;
        }

        var idSegment = value[prefix.Length..].Split('/', 2)[0];
        return Guid.TryParse(idSegment, out restaurantId);
    }

    private static List<string> ReadConfiguredAncestors(IConfiguration configuration)
    {
        var section = configuration.GetSection(ConfigurationKey);
        if (!string.IsNullOrWhiteSpace(section.Value))
        {
            return SplitAncestors(section.Value);
        }

        return section
            .GetChildren()
            .Select(child => child.Value?.Trim())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .ToList();
    }

    private static List<string> SplitAncestors(string value)
    {
        return value
            .Split([',', ';', ' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(ancestor => ancestor.Trim())
            .ToList();
    }

    private static bool IsValidAncestor(string value)
    {
        return value is "'self'" or "'none'" or "*" or "https:" or "http:" ||
            HttpOriginPattern().IsMatch(value);
    }

    [GeneratedRegex(
        @"^https?://(?:\*\.)?(?:localhost|[a-zA-Z0-9](?:[a-zA-Z0-9.-]*[a-zA-Z0-9])?)(?::(?:\d{1,5}|\*))?$",
        RegexOptions.CultureInvariant)]
    private static partial Regex HttpOriginPattern();
}
