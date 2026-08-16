using System.Text.Json.Serialization;
using Reach.Relay;

namespace Reach.Module;

[JsonSourceGenerationOptions()]
[JsonSerializable(typeof(RpcPayload))]
[JsonSerializable(typeof(RpcResult))]
[JsonSerializable(typeof(RemoteCommandRequest))]
[JsonSerializable(typeof(RemoteCommandSubmissionResult))]
[JsonSerializable(typeof(RemoteCommandSubmissionResult.Accepted))]
[JsonSerializable(typeof(RemoteCommandSubmissionResult.Failed))]
[JsonSerializable(typeof(RemoteCommandSubmissionError))]
[JsonSerializable(typeof(RelayCommand))]
[JsonSerializable(typeof(RelayCommandResult))]
internal sealed partial class ModuleJsonSerializerContext : JsonSerializerContext;
