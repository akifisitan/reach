using System.Collections.Frozen;
using Polly;
using Polly.Registry;
using Reach.Hosting;
using Reach.Module;
using Reach.Relay;

namespace Reach.Local.Server;

internal sealed class RemoteCommandCoordinator
{
    private readonly ResiliencePipeline _relayPostChaosPipeline;
    private readonly ResiliencePipeline _retryPipeline;
    private readonly ILogger<RemoteCommandCoordinator> _logger;
    private readonly ILocalRelay _relay;

    private readonly FrozenDictionary<Type, IRemoteCommandRpcHandler> _handlers;

    public RemoteCommandCoordinator(
        IEnumerable<IRemoteCommandRpcHandler> handlers,
        ResiliencePipelineProvider<string> resiliencePipelineProvider,
        ILogger<RemoteCommandCoordinator> logger,
        ILocalRelay relay
    )
    {
        _handlers = handlers.ToFrozenDictionary(x => x.CommandType);
        _relayPostChaosPipeline = resiliencePipelineProvider.GetPipeline(
            ChaosPolicyNames.RelayPost
        );
        _retryPipeline = resiliencePipelineProvider.GetPipeline(RetryPolicyNames.Default);
        _logger = logger;
        _relay = relay;
    }

    public async Task<RemoteCommandSubmissionResult> Submit(
        RemoteCommandRequest request,
        CancellationToken cancellationToken
    )
    {
        var commandType = request.Command.GetType();

        if (!_handlers.TryGetValue(commandType, out var handler))
        {
            throw new InvalidOperationException(
                $"No remote command RPC handler is registered for command type '{commandType.FullName}'. "
                    + "Register its handler with WithRemoteCommandRpcHandler<THandler>()."
            );
        }

        try
        {
            await _retryPipeline
                .ExecuteAsync(
                    retryToken =>
                        _relayPostChaosPipeline.ExecuteAsync(
                            relayToken => new ValueTask<PostToRemoteResult>(
                                _relay.PostToRemote(request.Command, relayToken)
                            ),
                            retryToken
                        ),
                    cancellationToken
                )
                .ConfigureAwait(false);

            _logger.ZLogInformation($"[{request.CorrelationId}] Remote command accepted by relay");

            return new RemoteCommandSubmissionResult.Accepted(request.CorrelationId);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"[{request.CorrelationId}] Failed to submit remote command");

            await Compensate(handler, request.Command, ex).ConfigureAwait(false);

            return new RemoteCommandSubmissionResult.Failed(
                request.CorrelationId,
                new RemoteCommandSubmissionError(
                    RemoteCommandSubmissionErrorCodes.SubmissionFailed,
                    "The remote command could not be submitted."
                )
            );
        }
    }

    private async Task Compensate(
        IRemoteCommandRpcHandler handler,
        RelayCommand command,
        Exception submissionException
    )
    {
        try
        {
            await handler
                .HandleSubmissionFailure(command, submissionException, CancellationToken.None)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.ZLogError(
                ex,
                $"[{command.Id}] Failed to compensate for remote command submission failure"
            );
        }
    }
}
