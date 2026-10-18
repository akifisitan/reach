using System.CommandLine;
using Reach.Module.SoapSearch.Features.SoapSearchStart;
using Reach.Module.SoapSearch.Features.SoapSearchStatus;

namespace Reach.Module.SoapSearch;

internal sealed class SoapSearchCliCommand : IRootCliCommand
{
    public Command Command { get; }

    public SoapSearchCliCommand(SoapSearchStartCliCommand start, SoapSearchStatusCliCommand status)
    {
        Command = new Command("soap-search", "Find SOAP request and response envelopes by SIID");
        start.AddTo(Command);
        status.AddTo(Command);
    }
}
