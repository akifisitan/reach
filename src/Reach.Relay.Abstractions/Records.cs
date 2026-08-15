namespace Reach.Relay;

public sealed record PostToRemoteResult(PollId PollId);

public sealed record PostToLocalResult();

public sealed record PollId(string Value);

public sealed record PollingOptions();

public record RelayCursor(long CurrentIndex, DateTimeOffset CreationTime, long PageOffset);
