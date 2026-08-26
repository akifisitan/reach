namespace Reach.Cli;

public interface IConsolePrompter
{
    string PromptText(string label, string? defaultValue = null);
    string PromptSecret(string label, bool allowEmpty = false);
}
