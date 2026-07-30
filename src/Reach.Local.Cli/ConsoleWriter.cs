using Reach.Module;

namespace Reach.Local.Cli;

internal sealed class ConsoleWriter : IConsoleWriter
{
    private static readonly Lock _lock = new();

    public void WriteLine(string message)
    {
        Console.WriteLine(message);
    }

    public void WriteLine(string message, ConsoleColor color)
    {
        lock (_lock)
        {
            Console.ResetColor();
            Console.BackgroundColor = color;
            Console.WriteLine(message);
            Console.ResetColor();
        }
    }
}
