using System.IO.Compression;
using Reach.Module.SoapSearch.Extraction;

namespace Reach.Module.SoapSearch.Tests;

internal sealed class SoapSearcherTests
{
    private static CancellationToken Token => TestContext.Current!.Execution.CancellationToken;
    private static readonly DateTimeOffset Start = new(2026, 10, 16, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset End = Start.AddHours(4);

    [Test]
    public async Task ReadsSeraraCacheAndFindsSampleThroughLiveDiscovery()
    {
        using var fixture = new Fixture();
        await fixture.WriteMetadata();
        var directory = fixture.Live("server1");
        await File.WriteAllTextAsync(
            Path.Combine(directory, "trace.log"),
            SampleSoapLog.Create(),
            Token
        );
        var result = await fixture.Searcher.Search(fixture.Request(SampleSoapLog.Siid), Token);
        await Assert.That(result.Outcome).IsEqualTo(SoapSearchOutcome.Complete);
        await Assert.That(result.Errors.Count).IsEqualTo(0);
    }

    [Test]
    public async Task StopsAtRequestFileInsteadOfPairingAcrossRotations()
    {
        using var fixture = new Fixture();
        await fixture.WriteMetadata();
        var directory = fixture.Live("server1");
        await File.WriteAllTextAsync(
            Path.Combine(directory, "trace.1.log"),
            SoapLogExtractorTests.Event(
                "target",
                "Servis request i aldı:",
                SoapLogExtractorTests.Envelope("request")
            ),
            Token
        );
        await File.WriteAllTextAsync(
            Path.Combine(directory, "trace.log"),
            SoapLogExtractorTests.Event(
                "target",
                "Servis cevap verdi:",
                SoapLogExtractorTests.Envelope("response")
            ),
            Token
        );
        File.SetLastWriteTimeUtc(
            Path.Combine(directory, "trace.1.log"),
            Start.AddHours(1).UtcDateTime
        );
        File.SetLastWriteTimeUtc(Path.Combine(directory, "trace.log"), End.UtcDateTime);
        var result = await fixture.Searcher.Search(fixture.Request(), Token);
        await Assert.That(result.Outcome).IsEqualTo(SoapSearchOutcome.Partial);
        await Assert.That(result.Request).IsNotNull();
        await Assert.That(result.Response).IsNull();
    }

    [Test]
    public async Task ContinuesAfterMissingServerDirectoryAndRetainsError()
    {
        using var fixture = new Fixture();
        await fixture.WriteMetadata();
        await File.WriteAllTextAsync(
            Path.Combine(fixture.Live("server2"), "trace.log"),
            Pair(),
            Token
        );
        var result = await fixture.Searcher.Search(
            fixture.Request() with
            {
                Servers = ["server1", "server2"],
            },
            Token
        );
        await Assert.That(result.Outcome).IsEqualTo(SoapSearchOutcome.Complete);
        await Assert.That(result.Errors.Count).IsEqualTo(1);
        await Assert
            .That(result.Errors[0].Source.Contains("server1", StringComparison.Ordinal))
            .IsTrue();
    }

    [Test]
    public async Task SearchesDatedZipEntriesAndContinuesAfterCorruptArchive()
    {
        using var fixture = new Fixture();
        await fixture.WriteMetadata();
        var brokenDirectory = Path.Combine(
            fixture.Options.ArchiveServerBasePath,
            "application",
            "SERVER1",
            "logs"
        );
        Directory.CreateDirectory(brokenDirectory);
        await File.WriteAllTextAsync(
            Path.Combine(brokenDirectory, "16.10.2026.zip"),
            "not a zip",
            Token
        );
        var directory = Path.Combine(
            fixture.Options.ArchiveServerBasePath,
            "application",
            "SERVER2",
            "logs"
        );
        Directory.CreateDirectory(directory);
        await using (
            var archive = await ZipFile.OpenAsync(
                Path.Combine(directory, "16.10.2026.zip"),
                ZipArchiveMode.Create,
                cancellationToken: Token
            )
        )
        {
            var ignored = archive.CreateEntry("trace.txt");
            ignored.LastWriteTime = End;
            await using (var writer = new StreamWriter(await ignored.OpenAsync(Token)))
            {
                await writer.WriteAsync(Pair(request: "ignored-text-entry").AsMemory(), Token);
            }

            var entry = archive.CreateEntry("folder/trace.log");
            entry.LastWriteTime = End;
            await using var output = new StreamWriter(await entry.OpenAsync(Token));
            await output.WriteAsync(Pair().AsMemory(), Token);
        }

        var result = await fixture.Searcher.Search(
            fixture.Request() with
            {
                Strategy = SoapSearchStrategy.Archive,
                Servers = ["server1", "server2"],
            },
            Token
        );
        await Assert.That(result.Outcome).IsEqualTo(SoapSearchOutcome.Complete);
        await Assert.That(result.Errors.Count).IsEqualTo(1);
        await Assert.That(result.Request!.Contains("request", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    [Arguments("trace.2.log", SoapSearchOutcome.NotFound)]
    [Arguments("trace.1.log", SoapSearchOutcome.Complete)]
    [Arguments("trace.log", SoapSearchOutcome.Complete)]
    [Arguments("trace.0.log", SoapSearchOutcome.NotFound)]
    [Arguments("15.10.2026.zip", SoapSearchOutcome.NotFound)]
    [Arguments("16.10.2026.zip", SoapSearchOutcome.Complete)]
    [Arguments("17.10.2026.zip", SoapSearchOutcome.NotFound)]
    [Arguments("arbitrary.zip", SoapSearchOutcome.NotFound)]
    public async Task SearchesOnlyFilesWhoseRotationIntervalOrZipDateOverlapsTheWindow(
        string siid,
        SoapSearchOutcome expected
    )
    {
        using var fixture = new Fixture();
        await fixture.WriteMetadata();
        var directory = fixture.Live("server1");
        foreach (
            var name in new[]
            {
                "trace.2.log",
                "trace.1.log",
                "trace.log",
                "trace.0.log",
                "15.10.2026.zip",
                "16.10.2026.zip",
                "17.10.2026.zip",
                "arbitrary.zip",
            }
        )
        {
            var path = Path.Combine(directory, name);
            if (name.EndsWith(".zip", StringComparison.Ordinal))
            {
                await using var archive = await ZipFile.OpenAsync(
                    path,
                    ZipArchiveMode.Create,
                    cancellationToken: Token
                );
                var entry = archive.CreateEntry("trace.log");
                entry.LastWriteTime = End;
                await using var writer = new StreamWriter(await entry.OpenAsync(Token));
                await writer.WriteAsync(Pair(name).AsMemory(), Token);
            }
            else
            {
                await File.WriteAllTextAsync(path, Pair(name), Token);
            }
        }

        File.SetLastWriteTimeUtc(
            Path.Combine(directory, "trace.2.log"),
            Start.AddHours(-1).UtcDateTime
        );
        File.SetLastWriteTimeUtc(
            Path.Combine(directory, "trace.1.log"),
            Start.AddHours(1).UtcDateTime
        );
        File.SetLastWriteTimeUtc(Path.Combine(directory, "trace.log"), End.AddHours(1).UtcDateTime);
        File.SetLastWriteTimeUtc(
            Path.Combine(directory, "trace.0.log"),
            End.AddHours(2).UtcDateTime
        );
        var result = await fixture.Searcher.Search(fixture.Request(siid), Token);
        await Assert.That(result.Outcome).IsEqualTo(expected);
        await Assert.That(result.Errors.Count).IsEqualTo(0);
    }

    [Test]
    public async Task MissingMetadataIsAnIncompleteSearchNotNoMatch()
    {
        using var fixture = new Fixture();
        var result = await fixture.Searcher.Search(fixture.Request(), Token);
        await Assert.That(result.Outcome).IsEqualTo(SoapSearchOutcome.IncompleteSearch);
        await Assert.That(result.Errors.Count).IsEqualTo(1);
    }

    [Test]
    public async Task InvalidMetadataIsReported()
    {
        using var fixture = new Fixture();
        await File.WriteAllTextAsync(fixture.Options.MetadataFilePath, "{}", Token);
        var result = await fixture.Searcher.Search(fixture.Request(), Token);
        await Assert.That(result.Outcome).IsEqualTo(SoapSearchOutcome.IncompleteSearch);
    }

    [Test]
    public async Task ValidatesSelectedServers()
    {
        using var fixture = new Fixture();
        await fixture.WriteMetadata();
        await Assert
            .That(async () =>
                await fixture.Searcher.Search(
                    fixture.Request() with
                    {
                        Servers = ["unknown"],
                    },
                    Token
                )
            )
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task NoMatchesWithoutErrorsIsNotFound()
    {
        using var fixture = new Fixture();
        await fixture.WriteMetadata();
        await File.WriteAllTextAsync(
            Path.Combine(fixture.Live("server1"), "trace.log"),
            Pair("other"),
            Token
        );
        var result = await fixture.Searcher.Search(fixture.Request(), Token);
        await Assert.That(result.Outcome).IsEqualTo(SoapSearchOutcome.NotFound);
        await Assert.That(result.Errors.Count).IsEqualTo(0);
    }

    private static string Pair(string siid = "target", string request = "request") =>
        SoapLogExtractorTests.Event(
            siid,
            "Servis request i aldı:",
            SoapLogExtractorTests.Envelope(request)
        )
        + SoapLogExtractorTests.Event(
            siid,
            "Servis cevap verdi:",
            SoapLogExtractorTests.Envelope("response")
        );

    private sealed class Fixture : IDisposable
    {
        private readonly string _root = Path.Combine(
            Path.GetTempPath(),
            "opencode",
            "reach-soap-tests",
            Guid.NewGuid().ToString("N")
        );
        public SoapSearchOptions Options { get; }
        public SoapSearcher Searcher { get; }

        public Fixture()
        {
            Directory.CreateDirectory(_root);
            Options = new SoapSearchOptions
            {
                MetadataFilePath = Path.Combine(_root, "cached-metadata.json"),
                LiveServerBasePath = Path.Combine(_root, "live"),
                ArchiveServerBasePath = Path.Combine(_root, "archive"),
            };
            Searcher = new SoapSearcher(
                Microsoft.Extensions.Options.Options.Create(Options),
                new SoapLogExtractor(new SoapEnvelopeFormatter())
            );
        }

        public string Live(string server)
        {
            var directory = Path.Combine(Options.LiveServerBasePath, server, "logs");
            Directory.CreateDirectory(directory);
            return directory;
        }

        public SoapSearchRequest Request(string siid = "target") =>
            new(siid, "application", ["server1"], Start, End, SoapSearchStrategy.Live);

        public Task WriteMetadata() =>
            File.WriteAllTextAsync(
                Options.MetadataFilePath,
                """
                [
                  {
                    "Name": "application",
                    "TeamName": "team",
                    "UK": "",
                    "ACI": "",
                    "LogPath": "logs",
                    "SearchTerm": "",
                    "Servers": ["server1", "server2"],
                    "LogSearchPattern": "",
                    "ArchiveDirectoryName": "application"
                  }
                ]
                """,
                Token
            );

        public void Dispose() => Directory.Delete(_root, recursive: true);
    }
}
