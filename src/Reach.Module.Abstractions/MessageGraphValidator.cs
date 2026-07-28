using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Reach.Relay;

namespace Reach.Module;

/// <summary>
/// Validates the message graph at startup so registration mistakes fail fast instead of at
/// first dispatch: duplicate handler registrations, and payload/result types that cross a
/// process boundary without being registered for polymorphic JSON serialization.
/// </summary>
public static class MessageGraphValidator
{
    public static void ValidateMessageGraph(this IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var descriptors = services.GetServices<HandlerDescriptor>().ToList();

        var jsonSerializerOptions = services.GetRequiredService<JsonSerializerOptions>();

        var errors = new List<string>();

        // CliCommandPayload/CliCommandResult never leave the process, so only duplicates are checked.
        ValidateHandlers<CliCommandPayload, CliCommandResult>(
            descriptors,
            jsonSerializerOptions,
            checkPayloadJsonRegistration: false,
            checkResultJsonRegistration: false,
            errors
        );

        ValidateHandlers<RpcPayload, RpcResult>(
            descriptors,
            jsonSerializerOptions,
            checkPayloadJsonRegistration: true,
            checkResultJsonRegistration: true,
            errors
        );

        ValidateHandlers<RelayCommand, RelayCommandResult>(
            descriptors,
            jsonSerializerOptions,
            checkPayloadJsonRegistration: true,
            checkResultJsonRegistration: true,
            errors
        );

        // RelayCommandResult handlers return Unit, which never leaves the process.
        ValidateHandlers<RelayCommandResult, Unit>(
            descriptors,
            jsonSerializerOptions,
            checkPayloadJsonRegistration: true,
            checkResultJsonRegistration: false,
            errors
        );

        // Discriminators live in one global table per base type (shared across modules), so
        // duplicates across modules are possible and only surface here.
        ValidateUniqueDiscriminators(jsonSerializerOptions, typeof(RpcPayload), errors);
        ValidateUniqueDiscriminators(jsonSerializerOptions, typeof(RpcResult), errors);
        ValidateUniqueDiscriminators(jsonSerializerOptions, typeof(RelayCommand), errors);
        ValidateUniqueDiscriminators(jsonSerializerOptions, typeof(RelayCommandResult), errors);

        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                $"Message graph validation failed:{Environment.NewLine} - "
                    + string.Join($"{Environment.NewLine} - ", errors)
            );
        }
    }

    private static void ValidateHandlers<TBasePayload, TBaseResult>(
        IReadOnlyList<HandlerDescriptor> descriptors,
        JsonSerializerOptions jsonSerializerOptions,
        bool checkPayloadJsonRegistration,
        bool checkResultJsonRegistration,
        List<string> errors
    )
    {
        var handlers = descriptors
            .Where(x =>
                x.BasePayloadType == typeof(TBasePayload) && x.BaseResultType == typeof(TBaseResult)
            )
            .ToList();

        foreach (var duplicate in handlers.GroupBy(x => x.PayloadType).Where(x => x.Count() > 1))
        {
            errors.Add(
                $"Multiple handlers registered for payload type '{duplicate.Key.FullName}': "
                    + string.Join(", ", duplicate.Select(x => x.HandlerType.FullName))
            );
        }

        foreach (var handler in handlers)
        {
            if (checkPayloadJsonRegistration)
            {
                ValidateJsonRegistration(
                    jsonSerializerOptions,
                    typeof(TBasePayload),
                    handler.PayloadType,
                    handler.HandlerType,
                    errors
                );
            }

            if (checkResultJsonRegistration)
            {
                ValidateJsonRegistration(
                    jsonSerializerOptions,
                    typeof(TBaseResult),
                    handler.ResultType,
                    handler.HandlerType,
                    errors
                );
            }
        }
    }

    private static void ValidateUniqueDiscriminators(
        JsonSerializerOptions jsonSerializerOptions,
        Type baseType,
        List<string> errors
    )
    {
        IList<JsonDerivedType>? derivedTypes;

        try
        {
            derivedTypes = jsonSerializerOptions
                .GetTypeInfo(baseType)
                .PolymorphismOptions?.DerivedTypes;
        }
        catch (Exception ex)
        {
            errors.Add(
                $"Could not resolve JSON metadata for base type '{baseType.FullName}': {ex.Message}"
            );

            return;
        }

        if (derivedTypes is null)
        {
            return;
        }

        foreach (
            var duplicate in derivedTypes
                .GroupBy(x => x.TypeDiscriminator)
                .Where(x => x.Count() > 1)
        )
        {
            errors.Add(
                $"Multiple derived types of '{baseType.FullName}' registered with type "
                    + $"discriminator '{duplicate.Key}': "
                    + string.Join(", ", duplicate.Select(x => x.DerivedType.FullName))
            );
        }
    }

    private static void ValidateJsonRegistration(
        JsonSerializerOptions jsonSerializerOptions,
        Type baseType,
        Type derivedType,
        Type handlerType,
        List<string> errors
    )
    {
        IList<JsonDerivedType>? derivedTypes;

        try
        {
            derivedTypes = jsonSerializerOptions
                .GetTypeInfo(baseType)
                .PolymorphismOptions?.DerivedTypes;
        }
        catch (Exception ex)
        {
            errors.Add(
                $"Could not resolve JSON metadata for base type '{baseType.FullName}': {ex.Message}"
            );

            return;
        }

        if (derivedTypes is null || derivedTypes.All(x => x.DerivedType != derivedType))
        {
            errors.Add(
                $"Handler '{handlerType.FullName}' uses '{derivedType.FullName}', which is not "
                    + $"registered as a derived type of '{baseType.FullName}' for polymorphic JSON "
                    + $"serialization. Register it in an {nameof(IWireJsonRegistration)} "
                    + $"implementation (e.g. via {nameof(WireJsonExtensions)}."
                    + $"{nameof(WireJsonExtensions.RegisterResultTypes)})."
            );
        }
    }
}
