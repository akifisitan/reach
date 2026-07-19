using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Reach.Relay.Abstractions;

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

    /// <summary>
    /// Registers a relay wire record (<see cref="RelayCommand" />/<see cref="RelayCommandResult" />)
    /// under its base type. The wire tag is the record's own
    /// <see cref="IWireRecord.Kind" />, carried in the "Kind" property on the wire.
    /// </summary>
    public static void RegisterWireRecord<TBase, TDerived>(this JsonTypeInfo typeInfo)
        where TDerived : TBase, IWireRecord =>
        typeInfo.RegisterDerivedType(typeof(TBase), typeof(TDerived), TDerived.Kind, "Kind");

    /// <summary>
    /// Registers a derived type under its base type with an explicit type discriminator,
    /// carried in the "$type" property on the wire.
    /// </summary>
    public static void RegisterDerivedType<TBase, TDerived>(
        this JsonTypeInfo typeInfo,
        string typeDiscriminator
    )
        where TDerived : TBase =>
        typeInfo.RegisterDerivedType(typeof(TBase), typeof(TDerived), typeDiscriminator, "$type");

    // Registers an abstract result type and its implementations for polymorphic serialization:
    // the abstract type and the implementations are registered under TBase, and the
    // implementations are registered under the abstract type as well, so (de)serialization
    // works regardless of which of the two is used as the declared type.
    public static void RegisterResultFamily<TBase, TResult>(
        this JsonTypeInfo typeInfo,
        string resultDiscriminator,
        params (Type Type, string Discriminator)[] resultTypes
    )
        where TResult : TBase
    {
        typeInfo.RegisterDerivedType(typeof(TBase), typeof(TResult), resultDiscriminator, "$type");

        foreach (var (resultType, discriminator) in resultTypes)
        {
            typeInfo.RegisterDerivedType(typeof(TBase), resultType, discriminator, "$type");
            typeInfo.RegisterDerivedType(typeof(TResult), resultType, discriminator, "$type");
        }
    }

    private static void RegisterDerivedType(
        this JsonTypeInfo typeInfo,
        Type baseType,
        Type derivedType,
        string typeDiscriminator,
        string discriminatorPropertyName
    )
    {
        if (typeInfo.Type != baseType)
        {
            return;
        }

        typeInfo.PolymorphismOptions ??= new JsonPolymorphismOptions
        {
            TypeDiscriminatorPropertyName = discriminatorPropertyName,
            IgnoreUnrecognizedTypeDiscriminators = false,
            UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToNearestAncestor,
        };

        typeInfo.PolymorphismOptions.DerivedTypes.Add(
            new JsonDerivedType(derivedType, typeDiscriminator)
        );
    }
}
