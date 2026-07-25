using System.Text.Json.Serialization;
using Reach.Relay;

namespace Reach.Plugin;

[JsonSourceGenerationOptions()]
[JsonSerializable(typeof(RpcPayload))]
[JsonSerializable(typeof(RpcResult))]
[JsonSerializable(typeof(RelayCommand))]
[JsonSerializable(typeof(RelayCommandResult))]
internal sealed partial class PluginJsonSerializerContext : JsonSerializerContext;
