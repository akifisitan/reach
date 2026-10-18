using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace Reach.Module.SoapSearch.Extraction;

internal sealed partial class SoapLogExtractor
{
    private readonly ISoapEnvelopeFormatter _formatter;

    public SoapLogExtractor(ISoapEnvelopeFormatter formatter)
    {
        _formatter = formatter;
    }

    private const string RequestMarker = "Servis request i aldı:";
    private const string ResponseMarker = "Servis cevap verdi:";

    public Task<SoapSearchResult> Extract(
        TextReader reader,
        string siid,
        string source = "log",
        CancellationToken cancellationToken = default
    ) => ExtractCore(reader, siid, source, null, false, cancellationToken);

    // Candidates belong to one server. Retain responses encountered before a
    // request because discovery order need not match event order across rotations.
    public Task<SoapSearchResult> ExtractCandidates(
        TextReader reader,
        string siid,
        string source,
        SoapSearchResult? previous = null,
        CancellationToken cancellationToken = default
    ) => ExtractCore(reader, siid, source, previous, true, cancellationToken);

    private async Task<SoapSearchResult> ExtractCore(
        TextReader reader,
        string siid,
        string source,
        SoapSearchResult? previous,
        bool includeUnpairedResponse,
        CancellationToken cancellationToken
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(siid);
        cancellationToken.ThrowIfCancellationRequested();
        var errors = new List<SoapSearchError>();
        var request = previous?.Request;
        var response = previous?.Response;
        if (request is not null && response is not null)
        {
            return new(request, response, SoapSearchOutcome.Complete, errors);
        }

        var eventText = new EnvelopeBuffer();
        var kind = EventKind.None;
        var lineNumber = 0;
        var eventLine = 0;

        void FinishEvent()
        {
            if (kind == EventKind.None)
            {
                return;
            }

            try
            {
                var envelope = _formatter.Format(eventText.ToString(), requireSoapEnvelope: true);
                if (kind == EventKind.Request)
                {
                    request ??= envelope;
                }
                else
                {
                    response ??= envelope;
                }
            }
            catch (XmlException ex)
            {
                errors.Add(
                    new(
                        source,
                        $"Invalid or truncated {kind.ToString().ToLowerInvariant()} envelope: {ex.Message}",
                        eventLine
                    )
                );
            }

            eventText = new EnvelopeBuffer();
            kind = EventKind.None;
        }

        try
        {
            await foreach (var line in ReadLines(reader, cancellationToken).ConfigureAwait(false))
            {
                lineNumber++;
                var header = Header().Match(line);
                if (header.Success)
                {
                    FinishEvent();
                    if (request is not null && response is not null)
                    {
                        return new(request, response, SoapSearchOutcome.Complete, errors);
                    }

                    if (!header.Groups["siid"].Value.Equals(siid, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    var message = line[header.Length..];
                    kind =
                        message.StartsWith(RequestMarker, StringComparison.Ordinal)
                        && request is null
                            ? EventKind.Request
                        : message.StartsWith(ResponseMarker, StringComparison.Ordinal)
                        && response is null
                        && (request is not null || includeUnpairedResponse)
                            ? EventKind.Response
                        : EventKind.None;
                    eventLine = lineNumber;
                    if (kind != EventKind.None && eventText.Append(message))
                    {
                        FinishEvent();
                    }
                }
                else if (kind != EventKind.None)
                {
                    if (eventText.Append(line))
                    {
                        FinishEvent();
                    }
                }

                if (request is not null && response is not null)
                {
                    return new(request, response, SoapSearchOutcome.Complete, errors);
                }
            }

            FinishEvent();
        }
        catch (Exception ex)
            when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            // Preserve complete candidates already read before an I/O failure.
            FinishEvent();
            errors.Add(new(source, ex.Message, lineNumber));
        }

        if (request is not null && response is null && !includeUnpairedResponse)
        {
            errors.Add(
                new(source, "No complete service response after the request in this log file.")
            );
        }

        var outcome = request is not null
            ? response is not null
                ? SoapSearchOutcome.Complete
                : SoapSearchOutcome.Partial
            : response is not null
                ? SoapSearchOutcome.Partial
                : errors.Count > 0
                    ? SoapSearchOutcome.IncompleteSearch
                    : SoapSearchOutcome.NotFound;
        return new(request, response, outcome, errors);
    }

    // ReadLineAsync discards line endings, which can be payload text. Retain them.
    private static async IAsyncEnumerable<string> ReadLines(
        TextReader reader,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        var buffer = new char[8192];
        var line = new StringBuilder();
        var count = 0;
        while (
            (
                count = await reader
                    .ReadAsync(buffer.AsMemory(), cancellationToken)
                    .ConfigureAwait(false)
            ) > 0
        )
        {
            for (var i = 0; i < count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                line.Append(buffer[i]);
                if (buffer[i] == '\n')
                {
                    yield return line.ToString();
                    line.Clear();
                }
            }
        }

        if (line.Length > 0)
        {
            yield return line.ToString();
        }
    }

    private enum EventKind
    {
        None,
        Request,
        Response,
    }

    [GeneratedRegex(
        @"^\[\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2},\d{3}\]\[[^\]\r\n]*\]\[[^\]\r\n]*\]\[[^\]\r\n]*\] - (?:\[(?<siid>[^\]\r\n]+)\])?"
    )]
    private static partial Regex Header();
}
