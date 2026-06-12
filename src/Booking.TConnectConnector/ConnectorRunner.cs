using System.Net;
using System.Net.Http.Json;

namespace Booking.TConnectConnector;

public sealed class ConnectorRunner(
    ConnectorOptions options,
    HttpClient httpClient,
    TConnectXmlParser parser)
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(options.InputDirectory);
        Directory.CreateDirectory(options.SuccessDirectory);
        Directory.CreateDirectory(options.FailedDirectory);

        Console.WriteLine($"Zambiq T-Connect Connector luistert naar: {options.InputDirectory}");
        Console.WriteLine($"Upload endpoint: {options.ApiUrl}");

        do
        {
            foreach (var file in Directory.EnumerateFiles(options.InputDirectory, "*.xml").Order())
            {
                await ProcessFileAsync(file, cancellationToken);
            }

            if (!options.RunOnce)
            {
                await Task.Delay(options.PollInterval, cancellationToken);
            }
        }
        while (!options.RunOnce && !cancellationToken.IsCancellationRequested);
    }

    private async Task ProcessFileAsync(string file, CancellationToken cancellationToken)
    {
        try
        {
            var xml = await File.ReadAllTextAsync(file, cancellationToken);
            var order = parser.Parse(xml);

            using var request = new HttpRequestMessage(HttpMethod.Post, options.ApiUrl)
            {
                Content = JsonContent.Create(order)
            };
            request.Headers.Add("X-Zambiq-Connector-Secret", options.Secret);

            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                Move(file, options.SuccessDirectory);
                Console.WriteLine($"Verwerkt: {Path.GetFileName(file)}");
                return;
            }

            if ((int)response.StatusCode is >= 400 and < 500)
            {
                Move(file, options.FailedDirectory);
                Console.Error.WriteLine(
                    $"Afgekeurd ({(int)response.StatusCode}): {Path.GetFileName(file)}");
                return;
            }

            Console.Error.WriteLine(
                $"API tijdelijk niet beschikbaar ({(int)response.StatusCode}); bestand blijft staan: {Path.GetFileName(file)}");
        }
        catch (Exception exception) when (exception is FormatException or System.Xml.XmlException)
        {
            Move(file, options.FailedDirectory);
            Console.Error.WriteLine($"Ongeldig XML-bestand {Path.GetFileName(file)}: {exception.Message}");
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException)
        {
            Console.Error.WriteLine($"Later opnieuw proberen voor {Path.GetFileName(file)}: {exception.Message}");
        }
    }

    private static void Move(string source, string targetDirectory)
    {
        var destination = Path.Combine(targetDirectory, Path.GetFileName(source));
        if (File.Exists(destination))
        {
            destination = Path.Combine(
                targetDirectory,
                $"{Path.GetFileNameWithoutExtension(source)}-{DateTime.UtcNow:yyyyMMddHHmmssfff}{Path.GetExtension(source)}");
        }

        File.Move(source, destination);
    }
}
