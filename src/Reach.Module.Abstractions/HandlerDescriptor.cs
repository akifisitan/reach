namespace Reach.Module;

/// <summary>
/// Describes a registered handler. Captured at registration time so the message graph can be
/// inspected (see <see cref="MessageGraphValidator" />) without instantiating any handler.
/// </summary>
internal sealed record HandlerDescriptor(
    Type HandlerType,
    Type BasePayloadType,
    Type BaseResultType,
    Type PayloadType,
    Type ResultType
)
{
    public static HandlerDescriptor Create<THandler, TBasePayload, TBaseResult>()
        where THandler : IHandler<TBasePayload, TBaseResult>
    {
        var (payloadType, resultType) = ResolvePayloadAndResultTypes(typeof(THandler));

        return new(
            typeof(THandler),
            typeof(TBasePayload),
            typeof(TBaseResult),
            payloadType,
            resultType
        );
    }

    private static (Type PayloadType, Type ResultType) ResolvePayloadAndResultTypes(
        Type handlerType
    )
    {
        for (var type = handlerType; type is not null; type = type.BaseType)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Handler<,,,>))
            {
                var genericArguments = type.GetGenericArguments();

                return (genericArguments[0], genericArguments[1]);
            }
        }

        throw new InvalidOperationException(
            $"Handler '{handlerType.FullName}' must derive from "
                + "Handler<TPayload, TResult, TBasePayload, TBaseResult> (e.g. via CliCommandHandler<,>, "
                + "RpcHandler<,>, RelayCommandHandler<,> or RelayCommandResultHandler<>) so its payload "
                + "and result types can be determined."
        );
    }
}
