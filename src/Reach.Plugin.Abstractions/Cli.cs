using System.CommandLine;

namespace Reach.Plugin.Abstractions;

public interface ICliCommand
{
    Command Command { get; }
}

public abstract record CliCommandPayload;

public abstract record CliCommandResult(int StatusCode);
