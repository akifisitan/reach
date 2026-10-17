namespace Reach.Module.Example.Echo;

internal interface IEchoFormatter
{
    string Format(string message);
}

internal sealed class EchoFormatter : IEchoFormatter
{
    public string Format(string message) => $"echo: {message}";
}
