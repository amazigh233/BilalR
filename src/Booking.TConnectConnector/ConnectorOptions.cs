namespace Booking.TConnectConnector;

public sealed record ConnectorOptions(
    Uri ApiUrl,
    string Secret,
    string InputDirectory,
    string SuccessDirectory,
    string FailedDirectory,
    TimeSpan PollInterval,
    bool RunOnce)
{
    public static ConnectorOptions FromEnvironmentAndArguments(string[] args)
    {
        var arguments = ReadArguments(args);
        var defaultRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            "Takeaway",
            "Tconnect",
            "temp");

        var apiUrl = Read("api-url", "ZAMBIQ_API_URL", arguments) ?? "http://localhost:5000";
        var secret = Read("secret", "ZAMBIQ_TCONNECT_SECRET", arguments);
        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new InvalidOperationException(
                "Connector secret ontbreekt. Stel ZAMBIQ_TCONNECT_SECRET in of gebruik --secret.");
        }

        var pollSeconds = int.TryParse(
            Read("poll-seconds", "ZAMBIQ_TCONNECT_POLL_SECONDS", arguments),
            out var parsedPollSeconds)
            ? Math.Clamp(parsedPollSeconds, 1, 30)
            : 2;

        return new ConnectorOptions(
            new Uri(apiUrl.TrimEnd('/') + "/api/delivery/thuisbezorgd/t-connect/orders"),
            secret,
            Read("in", "ZAMBIQ_TCONNECT_IN", arguments) ?? Path.Combine(defaultRoot, "in"),
            Read("ok", "ZAMBIQ_TCONNECT_OK", arguments) ?? Path.Combine(defaultRoot, "ok"),
            Read("nok", "ZAMBIQ_TCONNECT_NOK", arguments) ?? Path.Combine(defaultRoot, "nok"),
            TimeSpan.FromSeconds(pollSeconds),
            arguments.ContainsKey("once"));
    }

    private static Dictionary<string, string?> ReadArguments(string[] args)
    {
        var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < args.Length; index++)
        {
            var argument = args[index];
            if (!argument.StartsWith("--", StringComparison.Ordinal))
            {
                continue;
            }

            var key = argument[2..];
            var value = index + 1 < args.Length && !args[index + 1].StartsWith("--", StringComparison.Ordinal)
                ? args[++index]
                : null;
            result[key] = value;
        }

        return result;
    }

    private static string? Read(
        string argumentName,
        string environmentName,
        IReadOnlyDictionary<string, string?> arguments)
    {
        return arguments.TryGetValue(argumentName, out var argumentValue)
            ? argumentValue
            : Environment.GetEnvironmentVariable(environmentName);
    }
}
