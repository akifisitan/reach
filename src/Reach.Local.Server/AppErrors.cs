namespace Reach.Local.Server;

internal abstract class AppErrors(string Message) : Exception(Message)
{
    internal sealed class AlreadyProcessed() : AppErrors("Already stored this message");
}
