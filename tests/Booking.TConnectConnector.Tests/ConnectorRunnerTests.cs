using System.Net;
using Booking.TConnectConnector;

namespace Booking.TConnectConnector.Tests;

public sealed class ConnectorRunnerTests
{
    [Fact]
    public async Task RunOnce_UploadsXmlAndMovesItToOk()
    {
        var root = Path.Combine(Path.GetTempPath(), $"zambiq-tconnect-{Guid.NewGuid():N}");
        var input = Path.Combine(root, "in");
        var success = Path.Combine(root, "ok");
        var failed = Path.Combine(root, "nok");
        Directory.CreateDirectory(input);
        var sourceFile = Path.Combine(input, "order.xml");
        await File.WriteAllTextAsync(sourceFile, """
            <Order>
              <OrderId>TC-3001</OrderId>
              <Customer><Name>Test klant</Name></Customer>
              <Total currency="EUR">10.00</Total>
              <Items><Item><Name>Pizza</Name><Quantity>1</Quantity><UnitPrice>10.00</UnitPrice></Item></Items>
            </Order>
            """);

        var handler = new CapturingHandler();
        using var httpClient = new HttpClient(handler);
        var options = new ConnectorOptions(
            new Uri("https://api.example.test/api/delivery/thuisbezorgd/t-connect/orders"),
            "test-secret",
            input,
            success,
            failed,
            TimeSpan.FromSeconds(1),
            RunOnce: true);

        await new ConnectorRunner(options, httpClient, new TConnectXmlParser())
            .RunAsync(CancellationToken.None);

        Assert.False(File.Exists(sourceFile));
        Assert.True(File.Exists(Path.Combine(success, "order.xml")));
        Assert.False(File.Exists(Path.Combine(failed, "order.xml")));
        Assert.Equal("test-secret", handler.Secret);
        Assert.Contains("TC-3001", handler.Body);

        Directory.Delete(root, recursive: true);
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public string? Secret { get; private set; }

        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Secret = request.Headers.GetValues("X-Zambiq-Connector-Secret").Single();
            Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.Accepted);
        }
    }
}
