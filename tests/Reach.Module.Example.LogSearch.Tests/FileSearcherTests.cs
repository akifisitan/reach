namespace Reach.Module.Example.LogSearch.Tests;

internal sealed class FileSearcherTests
{
    [Test]
    public async Task SearchReturnsSimulatedResult()
    {
        var searcher = new FileSearcher(NullLogger<FileSearcher>.Instance);

        var result = await searcher.Search(
            "error",
            TestContext.Current!.Execution.CancellationToken
        );

        // The current implementation simulates a match rather than reading files.
        await Assert.That(Guid.TryParse(result.Content, out _)).IsTrue();
        await Assert.That(result.LineNumber).IsGreaterThanOrEqualTo(1);
        await Assert.That(result.LineNumber).IsLessThan(999999);
    }

    [Test]
    public async Task SearchHonorsCancellation()
    {
        var searcher = new FileSearcher(NullLogger<FileSearcher>.Instance);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert
            .That(async () =>
            {
                await searcher.Search("error", cancellation.Token);
            })
            .Throws<OperationCanceledException>();
    }
}
