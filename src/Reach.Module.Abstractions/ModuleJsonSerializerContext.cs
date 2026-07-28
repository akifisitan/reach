using System.Text.Json.Serialization;
using Reach.Relay;

namespace Reach.Module;

[JsonSourceGenerationOptions()]
[JsonSerializable(typeof(RpcPayload))]
[JsonSerializable(typeof(RpcResult))]
[JsonSerializable(typeof(RelayCommand))]
[JsonSerializable(typeof(RelayCommandResult))]
internal sealed partial class ModuleJsonSerializerContext : JsonSerializerContext;
