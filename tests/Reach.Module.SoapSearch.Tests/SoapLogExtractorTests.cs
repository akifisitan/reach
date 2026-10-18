using System.Text.RegularExpressions;
using System.Xml;
using Reach.Module.SoapSearch.Extraction;

namespace Reach.Module.SoapSearch.Tests;

internal sealed class SoapLogExtractorTests
{
    private static CancellationToken Token => TestContext.Current!.Execution.CancellationToken;

    [Test]
    public async Task ExtractsSampleServicePairWithoutDownstreamRequest()
    {
        using var reader = new StringReader(SampleSoapLog.Create());
        var result = await new SoapLogExtractor(new SoapEnvelopeFormatter()).Extract(
            reader,
            SampleSoapLog.Siid,
            cancellationToken: Token
        );

        await Assert.That(result.Outcome).IsEqualTo(SoapSearchOutcome.Complete);
        await Assert.That(result.Errors.Count).IsEqualTo(0);
        await Assert.That(result.Request).IsNotNull();
        await Assert.That(result.Response).IsNotNull();
        await AssertPreserved(SampleSoapLog.Request, result.Request!);
        await AssertPreserved(SampleSoapLog.Response, result.Response!);
        await Assert
            .That(result.Request!.Contains("\n    <soapenv:Header>", StringComparison.Ordinal))
            .IsTrue();
    }

    [Test]
    public async Task TruncatedSampleRequestIsReportedWithoutConsumingOtherEvents()
    {
        using var reader = new StringReader(SampleSoapLog.CreateTruncated());
        var result = await new SoapLogExtractor(new SoapEnvelopeFormatter()).Extract(
            reader,
            SampleSoapLog.Siid,
            cancellationToken: Token
        );

        await Assert.That(result.Request).IsNull();
        await Assert.That(result.Response).IsNull();
        await Assert.That(result.Outcome).IsEqualTo(SoapSearchOutcome.IncompleteSearch);
        await Assert.That(result.Errors.Count).IsEqualTo(1);
        await Assert.That(result.Errors[0].LineNumber).IsEqualTo(2);
    }

    [Test]
    public async Task MatchesExactHeaderSiidAndIgnoresPayloadAndDownstreamMatches()
    {
        var log =
            Event("target-long", "Servis request i aldı:", Envelope("target"))
            + Event("TARGET", "Servis request i aldı:", Envelope("target"))
            + Event("target", "Client request i göndermeden önce: Url:", Envelope("wrong"))
            + Event("other", "Servis cevap verdi:", Envelope("target"));
        var result = await Extract(log);
        await Assert.That(result.Outcome).IsEqualTo(SoapSearchOutcome.NotFound);
    }

    [Test]
    public async Task MissingResponseReturnsRequestAndNull()
    {
        var result = await Extract(Event("target", "Servis request i aldı:", Envelope("request")));
        await Assert.That(result.Outcome).IsEqualTo(SoapSearchOutcome.Partial);
        await Assert.That(result.Request).IsNotNull();
        await Assert.That(result.Response).IsNull();
        await Assert.That(result.Errors.Count).IsEqualTo(1);
    }

    [Test]
    public async Task TruncatedResponseDoesNotAbsorbNextEvent()
    {
        var result = await Extract(
            Event("target", "Servis request i aldı:", Envelope("request"))
                + Event(
                    "target",
                    "Servis cevap verdi:",
                    "<s:Envelope xmlns:s='http://schemas.xmlsoap.org/soap/envelope/'><s:Body>"
                )
                + Event("other", "Servis cevap verdi:", Envelope("wrong"))
        );
        await Assert.That(result.Outcome).IsEqualTo(SoapSearchOutcome.Partial);
        await Assert.That(result.Response).IsNull();
        await Assert.That(result.Errors.Any(x => x.LineNumber == 2)).IsTrue();
    }

    [Test]
    public async Task InterleavedEventsAndSoap12PrefixesAreHandled()
    {
        var response =
            "<Envelope xmlns='http://www.w3.org/2003/05/soap-envelope'><Body><result xmlns=''>ok</result></Body></Envelope>";
        var result = await Extract(
            Event("target", "Servis request i aldı:", Envelope("request"))
                + Event("other", "Servis request i aldı:", Envelope("wrong"))
                + Event("target", "Client request i göndermeden önce: Url:", "<broken")
                + Event("target", "Servis cevap verdi:", response)
        );
        await Assert.That(result.Outcome).IsEqualTo(SoapSearchOutcome.Complete);
        await AssertPreserved(response, result.Response!);
    }

    [Test]
    public async Task ResponseOnlyDoesNotBecomeARequest()
    {
        var result = await Extract(Event("target", "Servis cevap verdi:", Envelope("response")));
        await Assert.That(result.Request).IsNull();
        await Assert.That(result.Outcome).IsEqualTo(SoapSearchOutcome.NotFound);
    }

    [Test]
    public async Task PreservesLineEndingsInsidePayloadText()
    {
        var payload = "<value>one\r\ntwo\nthree &amp; &#x20;</value>";
        var result = await Extract(Event("target", "Servis request i aldı:", Envelope(payload)));
        await Assert.That(result.Request!.Contains(payload, StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task RejectsNonSoapXml()
    {
        var result = await Extract(
            Event("target", "Servis request i aldı:", "<Envelope><Body/></Envelope>")
        );
        await Assert.That(result.Request).IsNull();
        await Assert.That(result.Outcome).IsEqualTo(SoapSearchOutcome.IncompleteSearch);
    }

    [Test]
    public async Task ExtractsOnlyEnvelopeAndStopsBeforeLaterMalformedEvents()
    {
        var log =
            Event("target", "Servis request i aldı:", Envelope("request") + " trailing log details")
            + Event("target", "Servis cevap verdi:", Envelope("response") + " trailing log details")
            + Event("target", "Servis request i aldı:", "<broken");
        var result = await Extract(log);
        await Assert.That(result.Outcome).IsEqualTo(SoapSearchOutcome.Complete);
        await Assert.That(result.Errors.Count).IsEqualTo(0);
        await Assert
            .That(result.Response!.EndsWith("</s:Envelope>", StringComparison.Ordinal))
            .IsTrue();
    }

    [Test]
    public async Task EnvelopeBoundariesIgnoreClosingTagsInCdataAndComments()
    {
        var content =
            "<value a='>'><![CDATA[</s:Envelope>\n<fake/>]]><!-- </s:Envelope> --></value>";
        var result = await Extract(
            Event("target", "Servis request i aldı:", Envelope(content))
                + Event("target", "Servis cevap verdi:", Envelope("response"))
        );
        await Assert.That(result.Outcome).IsEqualTo(SoapSearchOutcome.Complete);
        await Assert.That(result.Request!.Contains(content, StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task CancellationPropagates()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        using var reader = new StringReader(
            Event("target", "Servis request i aldı:", Envelope("request"))
        );
        await Assert
            .That(async () =>
                await new SoapLogExtractor(new SoapEnvelopeFormatter()).Extract(
                    reader,
                    "target",
                    cancellationToken: cancellation.Token
                )
            )
            .Throws<OperationCanceledException>();
    }

    internal static string Event(string siid, string marker, string xml) =>
        $"[2026-10-16 14:13:57,177][          10][INFO ][Logger                   ] - [{siid}]{marker} {xml}\n";

    internal static string Envelope(string content) =>
        $"<s:Envelope xmlns:s='http://schemas.xmlsoap.org/soap/envelope/'><s:Body>{content}</s:Body></s:Envelope>";

    private static async Task<SoapSearchResult> Extract(string log)
    {
        using var reader = new StringReader(log);
        return await new SoapLogExtractor(new SoapEnvelopeFormatter()).Extract(
            reader,
            "target",
            cancellationToken: Token
        );
    }

    internal static async Task AssertPreserved(string original, string formatted)
    {
        // XmlReader compares values, names, prefixes and attribute order. Literal
        // comparisons additionally catch quote, entity and empty-tag rewrites.
        await Assert
            .That(string.Join("\0", ReadXml(formatted)))
            .IsEqualTo(string.Join("\0", ReadXml(original)));
        var originalLiterals = Regex
            .Matches(original, "\"[^\"]*\"|'[^']*'|&[^;]+;|/>")
            .Select(x => x.Value)
            .ToArray();
        var formattedLiterals = Regex
            .Matches(formatted, "\"[^\"]*\"|'[^']*'|&[^;]+;|/>")
            .Select(x => x.Value)
            .ToArray();
        await Assert
            .That(string.Join("\0", formattedLiterals))
            .IsEqualTo(string.Join("\0", originalLiterals));
    }

    private static List<string> ReadXml(string xml)
    {
        using var input = new StringReader(xml);
        using var reader = XmlReader.Create(
            input,
            new XmlReaderSettings { IgnoreWhitespace = true }
        );
        var result = new List<string>();
        while (reader.Read())
        {
            result.Add($"{reader.NodeType}|{reader.Name}|{reader.NamespaceURI}|{reader.Value}");
            if (reader.MoveToFirstAttribute())
            {
                do
                {
                    result.Add($"{reader.Name}|{reader.Value}");
                } while (reader.MoveToNextAttribute());
                reader.MoveToElement();
            }
        }

        return result;
    }
}
