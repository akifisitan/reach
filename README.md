# reach

## Architecture

Local Computer

`Reach.Local.Cli` ⇄ `Reach.Local.Server`

Remote Computer

`Reach.Remote.Agent`

Connection Flow

`Reach.Local.Cli` ⇄ `Reach.Local.Server` ⇄ `Relay` ⇄ `Reach.Remote.Agent`

The `Reach.Local.App` project publishes the `reach` executable. Run `reach serve` as a
foreground process to host the local server; other commands connect to that process over the
local RPC pipe.

Local RPC uses Nerdbank.JsonRpc with a generated `ILocalRpc` client proxy and
length-prefixed MessagePack over named pipes. The dependency is currently pinned to
the prerelease `0.2.1-alpha` package. Its transitive Nerdbank.Json analyzer requires
Roslyn 5.9, newer than the .NET 10 SDK compiler. `Directory.Build.targets` excludes
that JSON-specific analyzer; the RPC and PolyType source generators remain enabled.

## Publishing

Publish all deployable packages as single-file trimmed executables:

```powershell
./publish.ps1
```

Publish all deployable packages with native AOT:

```powershell
./publish.ps1 -Mode Aot
```

The script publishes `Reach.Local.App` and `Reach.Remote.Agent` to `artifacts/publish/<Mode>/<Runtime>/<ProjectName>`. The default runtime is `win-x64`; override it with `-Runtime`, for example:

```powershell
./publish.ps1 -Mode Regular -Runtime linux-x64
```

Publish output excludes `.pdb` and `.xml` files.

## Development

### Formatting

```bash
dotnet csharpier format .
```

```bash
dotnet format analyzers --diagnostics IDE0005 --no-restore
```
