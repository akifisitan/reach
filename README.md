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
