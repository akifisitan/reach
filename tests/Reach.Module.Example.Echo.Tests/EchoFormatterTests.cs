namespace Reach.Module.Example.Echo.Tests;

internal sealed class EchoFormatterTests
{
    [Test]
    [Arguments("hello world", "echo: hello world")]
    [Arguments("", "echo: ")]
    [Arguments("  hello\n世界  ", "echo:   hello\n世界  ")]
    public async Task FormatPreservesMessage(string message, string expected)
    {
        var formatter = new EchoFormatter();

        var reply = formatter.Format(message);

        await Assert.That(reply).IsEqualTo(expected);
    }
}
