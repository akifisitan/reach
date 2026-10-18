namespace Reach.Module.SoapSearch;

internal enum SoapSearchStrategy
{
    Live,
    Archive,
    Both,
}

internal sealed record SoapSearchRequest(
    string Siid,
    string Application,
    string[] Servers,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    SoapSearchStrategy Strategy
)
{
    public void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Siid);
        ArgumentException.ThrowIfNullOrWhiteSpace(Application);
        if (Servers is not { Length: > 0 } || Servers.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("At least one server is required.");
        }

        if (EndTime <= StartTime)
        {
            throw new ArgumentException("End time must be after start time.");
        }

        if (!Enum.IsDefined(Strategy))
        {
            throw new ArgumentException("Unknown search strategy.");
        }
    }
}

internal enum SoapSearchOutcome
{
    Complete,
    Partial,
    NotFound,
    IncompleteSearch,
}

internal sealed record SoapSearchError(string Source, string Message, int? LineNumber = null);

internal sealed record SoapSearchResult(
    string? Request,
    string? Response,
    SoapSearchOutcome Outcome,
    List<SoapSearchError> Errors
);
