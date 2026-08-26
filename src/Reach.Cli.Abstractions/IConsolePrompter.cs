namespace Reach.Cli;

public interface IConsolePrompter
{
    string PromptText(string label);
    string PromptSecret(string label);
}
