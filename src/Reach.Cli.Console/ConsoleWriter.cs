namespace Reach.Cli.Console;

public sealed class ConsoleWriter : IConsoleWriter
{
    public void WriteLine()
    {
        System.Console.WriteLine();
    }

    public void WriteLine(string message)
    {
        System.Console.WriteLine(message);
    }
}
