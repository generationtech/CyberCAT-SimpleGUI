# CP2077SaveExporter

Read-only command-line exporter for Cyberpunk 2077 save files (`.dat`). It uses the in-repo WolvenKit stack to parse saves and write JSON (`save.raw.json`, `save.enriched.json`) without involving the legacy CyberCAT GUI.

## Run (development)

From the repository root (so the `WolvenKit` project reference resolves):

```bash
dotnet run --project CP2077SaveExporter/CP2077SaveExporter.csproj -- /path/to/sav.dat
```

Or build and run the executable from `CP2077SaveExporter/bin/...` as usual for a .NET console app.

### Usage

```
CP2077SaveExporter [--mode raw|enriched|both] <path-to-sav.dat> [output-directory]
```

Default mode is `enriched`. If `output-directory` is omitted, files are written to the current working directory.

## Publish (Windows and Linux single-file)

Self-contained single-file builds are produced only via scripts in this folder; they do not change the solution’s default build.

From **`CP2077SaveExporter/`**:

- **Linux / Git Bash:** `./publish.sh`
- **Windows (PowerShell):** `.\publish.ps1`

Outputs:

- `publish/win-x64` — Windows x64
- `publish/linux-x64` — Linux x64

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download). The first publish may take a while while NuGet restores and the runtime is bundled.
