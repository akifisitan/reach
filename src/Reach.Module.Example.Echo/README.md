# Reach Example Echo Module

Small cloneable example module that exposes a top-level Echo CLI command and matching RPC/relay handlers.

```
reach echo -m "hello world"
```

Use this as the smallest template when creating a new Reach module. For a multi-step example with status polling, see `../Reach.Module.Example.LogSearch/README.md`.

Run the work directly through its isolated TUnit tests with `dotnet run --project tests/Reach.Module.Example.Echo.Tests` from the repository root. See [module testing instructions](../../../tests/README.md).
