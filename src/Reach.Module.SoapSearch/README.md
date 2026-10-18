# SOAP search

Find the inbound SOAP request and service response for an exact log-header SIID.
The module uses Reach's start/status workflow. `SoapSearcher.Search`,
`SoapSearcher.SearchFile`, `SoapLogExtractor.Extract`, and
`SoapEnvelopeFormatter.Format` can also be called independently within the module
and its test project.

## Configuration

Add this section to the executing remote agent's `config.json`:

```json
{
  "SoapSearch": {
    "MetadataFilePath": "cached-metadata.json",
    "LiveServerBasePath": "\\\\live-root",
    "ArchiveServerBasePath": "\\\\archive-root",
    "LogPathPrefix": "",
    "CreationTimeBufferInSeconds": 0,
    "ZipCreationTimeBufferInSeconds": 5
  }
}
```

Relative metadata paths resolve against the host's profile directory. Copy an
existing Serara `cached-metadata.json` there, or provide its absolute path. The
reader uses the same JSON array schema and source-generated deserialization:

```json
[
  {
    "Name": "ExampleApplication",
    "TeamName": "ExampleTeam",
    "UK": "",
    "ACI": "",
    "LogPath": "logs",
    "SearchTerm": "",
    "Servers": ["server1", "server2"],
    "LogSearchPattern": "",
    "ArchiveDirectoryName": "ExampleApplication"
  }
]
```

Application selection is case-insensitive. Selected servers must belong to the
application. Path construction follows Serara:

- Live: `LiveServerBasePath / server / LogPathPrefix / LogPath`
- Archive: `ArchiveServerBasePath / ArchiveDirectoryName / SERVER_UPPERCASE / LogPathPrefix / LogPath`

## Commands

```text
reach soap-search start --siid APP-API--261016141357-HKXFL --application ExampleApplication --servers server1 server2 --start-time 2026-10-16T14:00:00+03:00 --end-time 2026-10-16T15:00:00+03:00 --strategy Both
reach soap-search status --correlation-id <job-id>
```

The strategy is `Live`, `Archive`, or `Both`. The job ID returned by `start` is
separate from the SIID. Results use the existing SQLite result store with a
24-hour pending-job staleness threshold.

## Search and extraction

File discovery and ZIP entry filtering are adapted from Serara's
`WinFileEnumerator` and `ZipFileSearcher`. They group rotated logs by directory
and filename prefix, order by last-write time, and estimate each file's start
from the preceding file's last-write time minus the configured buffer. The oldest
file in a group has no lower bound. Date-named ZIPs use `dd.MM.yyyy.zip`.
Time bounds filter candidate files, not timestamps of individual events.

The search visits selected servers in order, live before archive. Inaccessible
directories and unreadable files or ZIP entries produce errors while remaining
sources are searched. Hidden, system, and reparse-point entries are skipped.

Extraction matches the bracketed SIID immediately after the log header's ` - `
using ordinal, case-sensitive equality. It recognizes these message prefixes:

- `Servis request i aldı:` for the inbound request.
- `Servis cevap verdi:` for the service response.

Downstream client calls are ignored. The search retains the first complete inbound
request and first complete service response for the SIID on each server. They may
come from different rotated files, ZIP entries, or live/archive roots when using
`Both`. A response encountered before the request is retained because discovery
order does not guarantee event order. Candidate file and ZIP entry time filtering
still applies; the search does not widen the requested window to find a response.

A complete pair ends the search immediately, even within a file or ZIP entry.
When the server containing the first complete request has no complete response in
any remaining candidate source, the search returns `Partial` with `Response = null`
and one missing-response error. It does not switch to another server's request or
pair across servers. Response-only servers do not stop the request search, and
their responses are discarded before searching the next server. No event-level
timestamp comparison is made between the request and response.

Standalone `SearchFile` and `Extract` retain single-file behavior: responses before
the request are ignored, and EOF without a response returns a partial request.
`ExtractCandidates` carries request/response candidates between independently read
sources. Its caller must keep that state scoped to one SIID and server. It reports
extraction errors but leaves missing-response reporting to the overall search.

Results distinguish `Complete`, `Partial`, `NotFound`, and `IncompleteSearch`.
Malformed or truncated envelopes generate extraction errors. A complete pair
can still carry errors from sources searched earlier. The request and response
fields contain only formatted SOAP envelopes, with no log-header text.

## XML preservation

The formatter follows the text-based approach and element/namespace layout of
[Josh Johnson's XML Tools v2](https://github.com/DotJoshJohnson/vscode-xml/blob/master/src/formatting/formatters/v2-xml-formatter.ts),
using four-space indentation. Subsequent namespace declarations go on new lines.
XML is validated but never reserialized through a DOM.

Prefixes, declarations, attribute order and values, quote styles, entity spelling,
empty-tag spelling, and text values are retained. Formatting changes whitespace
between elements and attributes. Mixed-content subtrees and `xml:space="preserve"`
subtrees remain verbatim. These preservation rules take precedence over XML Tools
behavior. DTDs and malformed XML are rejected rather than repaired.

## Validation

```text
dotnet run --project tests/Reach.Module.SoapSearch.Tests
dotnet csharpier format .
dotnet build
```

The tests generate synthetic logs with multiline SOAP envelopes, downstream calls,
interleaved events, and truncated requests. Search tests write these logs to temporary
files. Extraction tests check XML values, prefixes, attributes, and literal spellings
against the fixture envelopes. Tests also cover partial extraction, preservation,
rotation filtering, split-file and split-ZIP pairs in either scan order, live/archive
pairs, server isolation, error reporting, and wire serialization.
