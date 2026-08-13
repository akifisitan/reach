# reach

## Architecture

Local Computer

`Reach.Local.Cli` ⇄ `Reach.Local.Server`

Remote Computer

`Reach.Remote.Agent`

Connection Flow

`Reach.Local.Cli` ⇄ `Reach.Local.Server` ⇄ `Relay` ⇄ `Reach.Remote.Agent`

## Publishing

Publish all deployable packages as single-file trimmed executables:

```powershell
./publish.ps1
```

Publish all deployable packages with native AOT:

```powershell
./publish.ps1 -Mode Aot
```

The script publishes `Reach.Local.Cli`, `Reach.Local.Server`, and `Reach.Remote.Agent` to `artifacts/publish/<Mode>/<Runtime>/<ProjectName>`. The default runtime is `win-x64`; override it with `-Runtime`, for example:

```powershell
./publish.ps1 -Mode TrimmedReadyToRun -Runtime linux-x64
```

Publish output excludes `.pdb` and `.xml` files.
