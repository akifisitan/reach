namespace Reach.Module.SoapSearch.Features.SoapSearchStart;

internal sealed class SoapSearchCommandHandler
    : RelayCommandHandler<SoapSearchCommand, SoapSearchCommandResult>
{
    private readonly SoapSearcher _searcher;

    public SoapSearchCommandHandler(SoapSearcher searcher)
    {
        _searcher = searcher;
    }

    protected override async Task<SoapSearchCommandResult> Handle(
        SoapSearchCommand command,
        CancellationToken cancellationToken
    ) =>
        new(
            command.Id,
            await _searcher.Search(command.Request, cancellationToken).ConfigureAwait(false)
        );
}
