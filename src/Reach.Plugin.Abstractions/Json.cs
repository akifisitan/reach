using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Reach.Plugin.Abstractions;

public interface IJsonTypeInfoRegistration
{
    void Configure(JsonTypeInfo jsonTypeInfo);
    IJsonTypeInfoResolver Resolver { get; }
}

public static class PluginJsonExtensions
{
    public static JsonSerializerOptions CreateJsonSerializerOptions(
        IReadOnlyList<IJsonTypeInfoRegistration> jsonTypeInfoRegistrations
    )
    {
        var jsonSerializerOptions = new JsonSerializerOptions
        {
            RespectNullableAnnotations = true,
            RespectRequiredConstructorParameters = true,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        };

        jsonSerializerOptions.TypeInfoResolverChain.Add(
            PluginJsonSerializerContext.Default.WithAddedModifier(jsonTypeInfo =>
            {
                foreach (var jsonRegistration in jsonTypeInfoRegistrations)
                {
                    jsonRegistration.Configure(jsonTypeInfo);
                }
            })
        );

        foreach (var jsonRegistration in jsonTypeInfoRegistrations)
        {
            jsonSerializerOptions.TypeInfoResolverChain.Add(jsonRegistration.Resolver);
        }

        return jsonSerializerOptions;
    }

    public static void RegisterDerivedWithTypeNameAsDiscriminator<TBase, TDerived>(
        this JsonTypeInfo typeInfo
    )
        where TDerived : TBase
    {
        if (typeInfo.Type != typeof(TBase))
        {
            return;
        }

        typeInfo.PolymorphismOptions ??= new JsonPolymorphismOptions
        {
            TypeDiscriminatorPropertyName = "$type",
            IgnoreUnrecognizedTypeDiscriminators = false,
            UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToNearestAncestor,
        };

        typeInfo.PolymorphismOptions.DerivedTypes.Add(
            new JsonDerivedType(
                typeof(TDerived),
                typeof(TDerived).FullName ?? typeof(TDerived).Name
            ) // could read from map of sorts in the future
        );
    }
}
