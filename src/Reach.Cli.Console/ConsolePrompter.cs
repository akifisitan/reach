using System.Text;

namespace Reach.Cli.Console;

public sealed class ConsolePrompter : IConsolePrompter
{
    public string PromptText(string label)
    {
        while (true)
        {
            System.Console.Write($"{label}: ");

            var input = System.Console.ReadLine()?.Trim();

            if (!string.IsNullOrEmpty(input))
            {
                return input;
            }

            System.Console.WriteLine("Value cannot be empty.");
        }
    }

    public string PromptSecret(string label)
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

            System.Console.WriteLine("Value cannot be empty.");
        }
    }
}
