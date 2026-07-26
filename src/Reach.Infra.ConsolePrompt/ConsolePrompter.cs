using System.Text;

namespace Reach.Infra.ConsolePrompt;

public interface IConsolePrompter
{
    string PromptText(string label);
    string PromptSecret(string label);
}

public sealed class ConsolePrompter : IConsolePrompter
{
    public string PromptText(string label)
    {
        while (true)
        {
            Console.Write($"{label}: ");

            var input = Console.ReadLine()?.Trim();

            if (!string.IsNullOrEmpty(input))
            {
                return input;
            }

            Console.WriteLine("Value cannot be empty.");
        }
    }

    public string PromptSecret(string label)
    {
        while (true)
        {
            Console.Write($"{label}: ");

            var builder = new StringBuilder();

            while (true)
            {
                var key = Console.ReadKey(intercept: true);

                if (key.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    break;
                }

                if (key.Key == ConsoleKey.Backspace)
                {
                    if (builder.Length > 0)
                    {
                        builder.Remove(builder.Length - 1, 1);
                        Console.Write("\b \b");
                    }

                    continue;
                }

                if (char.IsControl(key.KeyChar))
                {
                    continue;
                }

                builder.Append(key.KeyChar);
                Console.Write('*');
            }

            if (builder.Length > 0)
            {
                return builder.ToString();
            }

            Console.WriteLine("Value cannot be empty.");
        }
    }
}
