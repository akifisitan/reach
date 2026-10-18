using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Reach.Module.SoapSearch.Features.SoapSearchStart;
using Reach.Module.SoapSearch.Features.SoapSearchStatus;
using Reach.Relay;

namespace Reach.Module.SoapSearch.Tests;

internal sealed class SoapSearchWireTests
{
    [Test]
    public async Task RoundTripsCommandsAndEnvelopeResultsThroughWireRegistration()
    {
        var options = WireJsonExtensions.CreateJsonSerializerOptions([
            new SoapSearchStartJsonRegistration(),
            new SoapSearchStatusJsonRegistration(),
        ]);
        var command = new SoapSearchCommand(
            "job-id",
            new(
                "siid",
                "app",
                ["server"],
                DateTimeOffset.UtcNow.AddHours(-1),
                DateTimeOffset.UtcNow,
                SoapSearchStrategy.Both
            )
        );
        var commandInfo = (JsonTypeInfo<RelayCommand>)options.GetTypeInfo(typeof(RelayCommand));
        var serialized = JsonSerializer.Serialize(command, commandInfo);
        var deserialized = (SoapSearchCommand)JsonSerializer.Deserialize(serialized, commandInfo)!;
        await Assert.That(deserialized.Request.Siid).IsEqualTo("siid");
        await Assert.That(deserialized.Id).IsEqualTo("job-id");

        var envelope = "<a xmlns='urn:example'>\n  &amp;&#x41;\n</a>";
        var result = new SoapSearchStatusResult.Success(
            new(envelope, null, SoapSearchOutcome.Partial, [new("file.log", "Missing response")])
        );
        var resultInfo = (JsonTypeInfo<RpcResult>)options.GetTypeInfo(typeof(RpcResult));
        var json = JsonSerializer.Serialize(result, resultInfo);
        var restored = (SoapSearchStatusResult.Success)
            JsonSerializer.Deserialize(json, resultInfo)!;
        await Assert.That(restored.Value.Request).IsEqualTo(envelope);
        await Assert.That(restored.Value.Response).IsNull();
        await Assert.That(restored.Value.Errors.Count).IsEqualTo(1);
    }
}
