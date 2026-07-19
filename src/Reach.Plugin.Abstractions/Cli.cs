using System.CommandLine;

namespace Reach.Plugin.Abstractions;

public interface ICliCommand
{
    Command Command { get; }
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
                var result = await handlerDispatcher.Handle(
                    payloadFactory(parseResult),
                    cancellationToken
                );

                return result.StatusCode;
            }
        );
    }
}
