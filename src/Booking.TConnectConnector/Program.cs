using Booking.TConnectConnector;

try
{
    var options = ConnectorOptions.FromEnvironmentAndArguments(args);
    using var httpClient = new HttpClient
    {
        Timeout = TimeSpan.FromSeconds(10)
    };
    var runner = new ConnectorRunner(options, httpClient, new TConnectXmlParser());
    using var cancellationTokenSource = new CancellationTokenSource();

    Console.CancelKeyPress += (_, eventArgs) =>
    {
        eventArgs.Cancel = true;
        cancellationTokenSource.Cancel();
    };

    await runner.RunAsync(cancellationTokenSource.Token);
}
catch (OperationCanceledException)
{
}
catch (Exception exception)
{
    Console.Error.WriteLine(exception.Message);
    Environment.ExitCode = 1;
}
