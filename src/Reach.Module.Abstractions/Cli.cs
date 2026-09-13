using System.CommandLine;

namespace Reach.Module;

public abstract class CliCommandHandler<TPayload, TResult>
    : Handler<TPayload, TResult, CliCommandPayload, CliCommandResult>
    where TPayload : CliCommandPayload
    where TResult : CliCommandResult;

public interface IRootCliCommand
{
    Command Command { get; }
}

public interface IChildCliCommand
{
    void AddTo(Command command);
}

public abstract record CliCommandPayload;

public abstract record CliCommandResult(int StatusCode);

public static class CliCommandExtensions
{
    /// <summary>
    /// Sets the command's action to build a payload from the parse result, dispatch it to its
    /// handler and return the handler result's status code.
    /// </summary>
    public static void SetPayloadAction<TPayload>(
        this Command command,
        Func<ParseResult, TPayload> payloadFactory,
        IHandlerDispatcher<CliCommandPayload, CliCommandResult> handlerDispatcher
    )
        where TPayload : CliCommandPayload
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(payloadFactory);
        ArgumentNullException.ThrowIfNull(handlerDispatcher);

        command.SetAction(
            async (parseResult, cancellationToken) =>
            {
                var result = await handlerDispatcher
                    .Handle(payloadFactory(parseResult), cancellationToken)
                    .ConfigureAwait(false);

                return result.StatusCode;
            }
        );
    }
}
