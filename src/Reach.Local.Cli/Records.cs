namespace Reach.Local.Cli;

internal sealed record AppOptions
{
    public bool DebugMode { get; set; }
}

internal sealed record LocalPipeRpcClientOptions
{
    public bool ShouldNotBootServer { get; set; }
    public required string ServerBinaryPath { get; set; }
}
