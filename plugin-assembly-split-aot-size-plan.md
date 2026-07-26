# Plugin Assembly Split Native AOT Size Plan

Goal: measure whether splitting plugin CLI/runtime assemblies materially reduces the Native AOT published size of the local CLI binary.

## 1. Establish Baseline

Publish the current CLI with Native AOT:

```powershell
dotnet publish src/Reach.Local.Cli/Reach.Local.Cli.csproj -c Release -o artifacts/publish/baseline-cli-aot -p:PublishAot=true
```

Measure the full publish directory:

```powershell
(Get-ChildItem artifacts/publish/baseline-cli-aot -Recurse | Measure-Object Length -Sum).Sum
```

Record the output files:

```powershell
Get-ChildItem artifacts/publish/baseline-cli-aot -Recurse | Select-Object FullName, Length
```

Also record the main executable size:

```powershell
Get-ChildItem artifacts/publish/baseline-cli-aot -File | Sort-Object Length -Descending | Select-Object Name, Length
```

## 2. Create Experimental Split

For one plugin only, probably `Reach.Plugin.Example`, create separate assemblies:

```text
Reach.Plugin.Example.Contracts
Reach.Plugin.Example.Cli
Reach.Plugin.Example.Runtime
```

Move code by surface:

```text
Contracts:
- payload/result records
- JSON registrations
- shared constants/types

Cli:
- CLI command classes
- AddExamplePluginCli()

Runtime:
- RPC handlers
- relay handlers
- result handlers
- result store registration
- AddExamplePluginRuntime()
```

Update references:

```text
Reach.Local.Cli -> Example.Contracts + Example.Cli
Reach.Local.Server -> Example.Contracts + Example.Runtime
Reach.Remote.Agent -> Example.Contracts + Example.Runtime
```

Do not split every plugin initially. One plugin is enough to estimate the size win and plugin-authoring friction.

## 3. Publish Experimental CLI

Publish the split version with Native AOT:

```powershell
dotnet publish src/Reach.Local.Cli/Reach.Local.Cli.csproj -c Release -o artifacts/publish/split-cli-aot -p:PublishAot=true
```

Measure the full publish directory:

```powershell
(Get-ChildItem artifacts/publish/split-cli-aot -Recurse | Measure-Object Length -Sum).Sum
```

Record output files:

```powershell
Get-ChildItem artifacts/publish/split-cli-aot -Recurse | Select-Object FullName, Length
```

Record the main executable size:

```powershell
Get-ChildItem artifacts/publish/split-cli-aot -File | Sort-Object Length -Descending | Select-Object Name, Length
```

Expected result: runtime-only plugin code and dependencies should no longer contribute to the CLI Native AOT binary if no CLI path references them.

## 4. Compare Outputs

Compare total publish sizes:

```powershell
$baseline = (Get-ChildItem artifacts/publish/baseline-cli-aot -Recurse | Measure-Object Length -Sum).Sum
$split = (Get-ChildItem artifacts/publish/split-cli-aot -Recurse | Measure-Object Length -Sum).Sum
[pscustomobject]@{
    BaselineBytes = $baseline
    SplitBytes = $split
    SavedBytes = $baseline - $split
    SavedPercent = if ($baseline -gt 0) { (($baseline - $split) / $baseline) * 100 } else { 0 }
}
```

Compare the largest files:

```powershell
Get-ChildItem artifacts/publish/baseline-cli-aot -Recurse -File | Sort-Object Length -Descending | Select-Object -First 20 FullName, Length
Get-ChildItem artifacts/publish/split-cli-aot -Recurse -File | Sort-Object Length -Descending | Select-Object -First 20 FullName, Length
```

## 5. Evaluate Authoring Cost

Track the practical cost while doing the experiment:

- How many files moved?
- How many project references changed?
- Did JSON registration become awkward?
- Did `internal` visibility become painful?
- Did feature registration become harder to understand?
- Did plugin authoring feel worse?

## Decision Rule

Adopt split assemblies only if Native AOT size drops enough to justify the extra plugin-authoring overhead.

Keep one plugin project with `AddXPluginCli()` and `AddXPluginRuntime()` surfaces if the size win is small or the split makes plugin authoring noticeably worse.
