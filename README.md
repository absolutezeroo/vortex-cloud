# Vortex Cloud

## 5-Minute Quickstart
1. Clone and enter the repo.
2. Run bootstrap once.
3. Set your local DB connection string.
4. Run the app in Development mode.

Clone:

```bash
git clone <your-repo-url> vortex-cloud
cd vortex-cloud
```

Bootstrap (PowerShell):

```powershell
pwsh -File scripts/bootstrap.ps1
```

Bootstrap (bash/zsh):

```bash
sh scripts/bootstrap.sh
```

Set `Vortex:Database:ConnectionString` in `appsettings.Development.json`, then run:

PowerShell:

```powershell
$env:DOTNET_ENVIRONMENT="Development"; dotnet run --project Vortex.Main/Vortex.Main.csproj
```

bash/zsh:

```bash
DOTNET_ENVIRONMENT=Development dotnet run --project Vortex.Main/Vortex.Main.csproj
```

## What This Repository Is
`Vortex.Cloud.sln` is the main Vortex emulator solution.
It includes the host executable (`Vortex.Main`), domain modules (`Vortex.Rooms`, `Vortex.Players`, `Vortex.Database`, and others), networking/message layers, and plugin infrastructure.

## Tooling Baseline
- .NET SDK 9.x (pinned via `global.json`)
- Git
- MySQL running locally (or reachable dev instance)

Check SDK:

```bash
dotnet --version
```

## Local Configuration
- `appsettings.json` contains shared defaults.
- `appsettings.Development.json` is local-only and gitignored.
- The bootstrap script creates `appsettings.Development.json` from `appsettings.json` if missing.

## Quality Model (Two-Phase)
- Fast local commit check:
  - `dotnet build Vortex.Main/Vortex.Main.csproj -t:VortexCloudFastCheck`
- Full quality gate (pre-push + CI):
  - `dotnet build Vortex.Main/Vortex.Main.csproj -t:VortexCloudQualityGate`
- AI policy rollout phase:
  - Default is `VortexAIPolicyPhase=1` (warn-first).
  - Preview strict mode with `-p:VortexAIPolicyPhase=2`.

Hooks are repository-managed in `.githooks`:
- `pre-commit` runs the fast check.
- `pre-push` runs the full quality gate.

## Build Scope Matrix
| Command | Scope | Default? | Use when |
| --- | --- | --- | --- |
| `dotnet build Vortex.Main/Vortex.Main.csproj` | Core emulator only | Yes | Normal core development and CI-compatible local checks |
| `dotnet build Vortex.Cloud.sln` | All projects currently in solution (including sample plugin) | No | One-window integrated core + plugin work |
| `dotnet build ../turbo-sample-plugin/TurboSamplePlugin/TurboSamplePlugin.csproj` | Sample plugin only | No | Plugin-only iteration |

`TurboSamplePlugin` intentionally stays in `Vortex.Cloud.sln` for IDE convenience, but the default repo build contract is project-scoped to `Vortex.Main`.

## Daily Commands
- Core build (default): `dotnet build Vortex.Main/Vortex.Main.csproj`
- Integrated solution build (optional): `dotnet build Vortex.Cloud.sln`
- Plugin build only (optional): `dotnet build ../turbo-sample-plugin/TurboSamplePlugin/TurboSamplePlugin.csproj`
- Fast checks: `dotnet build Vortex.Main/Vortex.Main.csproj -t:VortexCloudFastCheck`
- Full quality gate: `dotnet build Vortex.Main/Vortex.Main.csproj -t:VortexCloudQualityGate`
- Run in Development: `dotnet run --project Vortex.Main/Vortex.Main.csproj`

## Local Dev Plugins
Plugin loading supports both the runtime plugin folder and dev-specific paths:
- Default folder: `<runtime>/plugins`
- Optional config: `Vortex:Plugin:DevPluginPaths`

Example:

```json
{
  "Vortex": {
    "Plugin": {
      "DevPluginPaths": [
        "C:/Users/you/RiderProjects/turbo-sample-plugin/TurboSamplePlugin/bin/Debug/net10.0"
      ]
    }
  }
}
```

If the same plugin key exists in both places, `DevPluginPaths` wins and a warning is logged.

### Plugin project setup
Your plugin's `.csproj` must copy `manifest.json` to the build output:

```xml
<ItemGroup>
  <Content Include="manifest.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </Content>
</ItemGroup>
```

You can list multiple plugin paths in `DevPluginPaths` when developing several plugins at once.

### Two-terminal dev workflow
Terminal 1 (run emulator):

```bash
dotnet run --project Vortex.Main
```

Terminal 2 (watch plugin):

```bash
cd C:/path/to/your-plugin
dotnet watch build
```

When `dotnet watch` rebuilds your plugin, Vortex Cloud detects the new DLL and hot-reloads the plugin in-process.

### Plugin hot-reload limitations
- Grain types cannot be hot-reloaded because Orleans grain type registration happens at silo startup.
- Memory may grow over many reloads if assembly references are retained; restart periodically during long sessions.

### Integrated plugin dev loop (single terminal flow)
Canonical integrated workflow lives in the plugin repo:
- Guide: `../turbo-sample-plugin/README.md`
- PowerShell: `pwsh -File ../turbo-sample-plugin/scripts/dev-integrated.ps1`
- bash/zsh: `sh ../turbo-sample-plugin/scripts/dev-integrated.sh`

## Orleans Notes
Vortex Cloud uses Orleans as its core runtime model for stateful domain workflows.
For project-specific Orleans guidance, see `docs/orleans.md`.
For a concrete end-to-end trace of a single packet through handlers, grains, and streams, see `docs/walkthroughs/request-lifecycle.md`.

## Troubleshooting
### MySQL connection errors
If you see `Unable to connect to any of the specified MySQL hosts`:
1. Verify `Vortex:Database:ConnectionString` in `appsettings.Development.json`.
2. Verify MySQL host/port are reachable.
3. Verify no `VORTEX__...` environment variables override your local setting.

### Development file not loading
1. Ensure `DOTNET_ENVIRONMENT=Development` when running.
2. Confirm `appsettings.Development.json` exists at repo root.

### Quality check failures
1. Run `dotnet tool restore`.
2. Run `dotnet csharpier .`.
3. Run `dotnet format style`.
4. Run `dotnet format analyzers`.
5. Re-run `dotnet build Vortex.Main/Vortex.Main.csproj -t:VortexCloudQualityGate`.

### Solution build fails but core build passes
If `dotnet build Vortex.Cloud.sln` fails because of plugin project state, use the default core build command:

```bash
dotnet build Vortex.Main/Vortex.Main.csproj
```

## AI-Assisted Development
Canonical AI context files:
- `AGENTS.md` (coding contract and review rules)
- `CONTEXT.md` (architecture boundaries and placement rules)
- `docs/patterns/` (golden implementation examples)
- `docs/walkthroughs/request-lifecycle.md` (one packet, socket to client — the real flow)
- `docs/walkthroughs/add-a-feature.md` (adding a feature, layer by layer)
- `docs/patterns/vertical-slice.md` (handler + grain + test on a single feature)
- `docs/glossary.md` (Habbo + Orleans terminology, each term mapped to its file)

Planning & design references:
- `ROADMAP.md` (completion plan: epics → stories → Definition of Done)
- `DATA-MODEL.md` (authoritative schema for tables not yet in the codebase — groups, rentable space, pets, bots, … — plus naming conventions)
- `PETS-DESIGN.md` (pet implementation: autonomous-agent behavior, state machine, persistence)

Tool-specific adapters:
- `.github/copilot-instructions.md` (GitHub Copilot)
- `CLAUDE.md` (Claude)
- `CODEX.md` (Codex)

Prompt recipe for any AI tool:
1. Include task + exact target file paths.
2. Attach `AGENTS.md` and `CONTEXT.md`.
3. Reference one relevant file from `docs/patterns/` or a walkthrough from `docs/walkthroughs/` (and the relevant `DATA-MODEL.md` section for schema work).
4. Ask for edge-case handling and validation commands.

Boost-style prompting pack:
- portable prompt contract + task recipes live in `AGENTS.md`
- architecture invariants live in `CONTEXT.md`
- tool adapters live in `.github/copilot-instructions.md`, `CLAUDE.md`, and `CODEX.md`
