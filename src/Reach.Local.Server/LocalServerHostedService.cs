using Microsoft.Extensions.Hosting;

namespace Reach.Local.Server;

internal sealed class LocalServerHostedService : BackgroundService
{
    private readonly LocalServer _localServer;

    public LocalServerHostedService(LocalServer localServer)
    {
        _localServer = localServer;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return _localServer.Start(stoppingToken);
    }
}
