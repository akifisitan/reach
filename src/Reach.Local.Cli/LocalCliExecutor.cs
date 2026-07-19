namespace Reach.Local.Cli;

internal sealed class LocalCliExecutor
{
    private readonly RootCliCommand _rootCliCommand;

    public LocalCliExecutor(RootCliCommand rootCliCommand)
    {
        _rootCliCommand = rootCliCommand;
    }

    public Task Execute(string[] args) => _rootCliCommand.Invoke(args);
}
