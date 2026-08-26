using System.Text;

namespace Reach.Cli.Console;

public sealed class ConsolePrompter : IConsolePrompter
{
    public string PromptText(string label, string? defaultValue = null)
    {
        while (true)
        {
            var defaultLabel = defaultValue is null ? string.Empty : $" [{defaultValue}]";
            System.Console.Write($"{label}{defaultLabel}: ");

            var input = System.Console.ReadLine()?.Trim();

            if (!string.IsNullOrEmpty(input))
            {
                return input;
            }

            if (defaultValue is not null)
            {
                return defaultValue;
            }

            System.Console.WriteLine("Value cannot be empty.");
        }
    }

    public string PromptSecret(string label, bool allowEmpty = false)
    {
        while (true)
        {
            System.Console.Write($"{label}: ");

            var builder = new StringBuilder();

            while (true)
            {
                var key = System.Console.ReadKey(intercept: true);

                if (key.Key == ConsoleKey.Enter)
                {
                    System.Console.WriteLine();
                    break;
                }

                if (key.Key == ConsoleKey.Backspace)
                {
                    if (builder.Length > 0)
                    {
                        builder.Remove(builder.Length - 1, 1);
                        System.Console.Write("\b \b");
                    }

                    continue;
                }

                if (char.IsControl(key.KeyChar))
                {
                    continue;
                }

                builder.Append(key.KeyChar);
                System.Console.Write('*');
            }

            if (builder.Length > 0)
            {
                return builder.ToString();
            }

            if (allowEmpty)
            {
                return string.Empty;
            }

            System.Console.WriteLine("Value cannot be empty.");
        }
    }
}
