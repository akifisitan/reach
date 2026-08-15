using Microsoft.Extensions.Hosting;

namespace Reach.Remote.Agent;

internal sealed class RemoteAgentHostedService : BackgroundService
{
    private readonly RemoteAgent _remoteAgent;

    public RemoteAgentHostedService(RemoteAgent remoteAgent)
    {
        _remoteAgent = remoteAgent;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return _remoteAgent.Start(stoppingToken);
    }
}
