using Microsoft.Extensions.Hosting;

namespace Reach.Remote.Agent;

internal sealed class RemoteAgentHostedService : IHostedService
{
    private readonly RemoteAgent _remoteAgent;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;

    public RemoteAgentHostedService(
        RemoteAgent remoteAgent,
        IHostApplicationLifetime hostApplicationLifetime
    )
    {
        _remoteAgent = remoteAgent;
        _hostApplicationLifetime = hostApplicationLifetime;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // In case simulation is needed
        //using var cts = new CancellationTokenSource();

        //Console.CancelKeyPress += (_, e) =>
        //{
        //    e.Cancel = true;

        //    cts.Cancel();
        //};

        //cancellationToken = cts.Token;

        await _remoteAgent.Start(cancellationToken);

        _hostApplicationLifetime.StopApplication();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
