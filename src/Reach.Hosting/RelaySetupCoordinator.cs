using Reach.Relay;

namespace Reach.Hosting;

public sealed class RelaySetupCoordinator
{
    private readonly string _appBasePath;
    private readonly IReadOnlyList<IRelaySetup> _relaySetupList;

    public RelaySetupCoordinator(string appBasePath, IEnumerable<IRelaySetup> relaySetupList)
    {
        _appBasePath = appBasePath;
        _relaySetupList = relaySetupList.ToList();
    }

    public async Task EnsureSetup(CancellationToken cancellationToken = default)
    {
        foreach (var setup in _relaySetupList)
        {
            if (!setup.IsComplete(_appBasePath))
            {
                await Run(setup, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    public async Task RunSetup(CancellationToken cancellationToken = default)
    {
        foreach (var setup in _relaySetupList)
        {
            await Run(setup, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task Run(IRelaySetup setup, CancellationToken cancellationToken)
    {
        var exitCode = await setup.Run(_appBasePath, cancellationToken).ConfigureAwait(false);

        if (exitCode != 0)
        {
            throw new InvalidOperationException(
                $"Relay setup '{setup.Name}' did not complete successfully."
            );
        }
    }
}
