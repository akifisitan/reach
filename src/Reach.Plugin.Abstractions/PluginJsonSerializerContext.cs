using System.Text.Json.Serialization;
using Reach.Relay.Abstractions;

namespace Reach.Plugin.Abstractions;

[JsonSourceGenerationOptions()]
[JsonSerializable(typeof(RpcPayload))]
[JsonSerializable(typeof(RpcResult))]
[JsonSerializable(typeof(RelayCommand))]
[JsonSerializable(typeof(RelayCommandResult))]
internal sealed partial class PluginJsonSerializerContext : JsonSerializerContext;
