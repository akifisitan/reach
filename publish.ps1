param(
    [ValidateSet('Aot', 'Regular')]
    [string] $Mode = 'Regular',
    [string] $Runtime = 'win-x64',
    [string] $Configuration = 'Release',
    [string] $OutputRoot = (Join-Path $PSScriptRoot 'artifacts/publish')
)

$ErrorActionPreference = 'Stop'

$commonPublishProperties = @()

$projects = @(
    'src/Reach.Local.Cli/Reach.Local.Cli.csproj',
    'src/Reach.Local.Server/Reach.Local.Server.csproj',
    'src/Reach.Remote.Agent/Reach.Remote.Agent.csproj'
)

$publishProperties = switch ($Mode) {
    'Aot' {
        @(
            '-p:PublishAot=true'
        )
    }
    'Regular' {
        @(
            '-p:PublishAot=false',
            '-p:PublishSingleFile=true',
            '-p:PublishTrimmed=true',
            '-p:EnableCompressionInSingleFile=true'
        )
    }
}

foreach ($project in $projects) {
    $projectName = [System.IO.Path]::GetFileNameWithoutExtension($project)
    $outputPath = Join-Path $OutputRoot (Join-Path $Mode (Join-Path $Runtime $projectName))

    $publishArgs = @(
        'publish',
        $project,
        '--configuration',
        $Configuration,
        '--runtime',
        $Runtime,
        '--self-contained',
        'true',
        '--output',
        $outputPath
    ) + $commonPublishProperties + $publishProperties

    dotnet @publishArgs

    Get-ChildItem -LiteralPath $outputPath -Include '*.pdb', '*.xml' -File -Recurse | Remove-Item -Force
}
