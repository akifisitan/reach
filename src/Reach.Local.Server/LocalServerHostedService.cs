using Microsoft.Extensions.Hosting;

namespace Reach.Local.Server;

internal sealed class LocalServerHostedService : IHostedService
{
    private readonly LocalServer _localServer;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;

    public LocalServerHostedService(
        LocalServer localServer,
        IHostApplicationLifetime hostApplicationLifetime
    )
    {
        _localServer = localServer;
        _hostApplicationLifetime = hostApplicationLifetime;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _localServer.Start(cancellationToken);

        _hostApplicationLifetime.StopApplication();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
