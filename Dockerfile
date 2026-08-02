# syntax=docker/dockerfile:1

# =================================================================================================
# Build stage
# =================================================================================================
# global.json pins the SDK to "10.0" with rollForward "latestFeature", so the image must carry a
# .NET 10 SDK: a 9.x SDK cannot satisfy that pin and every dotnet command would fail up front with
# "compatible .NET SDK was not found". EF Core / Pomelo staying on the .NET 9 package line (see
# AGENTS.md) does not change this — those are NuGet packages running on the net10.0 runtime.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# --- restore layer -------------------------------------------------------------------------------
# Only the files that describe the dependency graph are copied here. As long as no project file and
# no pinned package version changes, Docker reuses this layer and the NuGet packages it downloaded,
# so editing source code no longer costs a full restore. Copying the sources first would invalidate
# it on every single edit — which is the whole point of splitting the two.
COPY global.json Directory.Build.props Directory.Build.targets Directory.Packages.props ./

# COPY flattens every match into the destination directory, so the project files land side by side
# in /src and are then moved back under the directory that carries their name. Every project in this
# repository lives at <Name>/<Name>.csproj; if that convention is ever broken, the restore below
# fails loudly with a missing-project error rather than silently building something else.
COPY */*.csproj ./
RUN for proj in *.csproj; do \
        mkdir -p "${proj%.csproj}" && mv "$proj" "${proj%.csproj}/"; \
    done

# Restoring Vortex.Main pulls in its full ProjectReference graph. The test projects are copied above
# but deliberately not restored — the image only ships the host.
RUN dotnet restore Vortex.Main/Vortex.Main.csproj

# --- source + publish ----------------------------------------------------------------------------
COPY . .

# --no-restore keeps the cached restore layer authoritative: without it the publish would hit the
# network again and the layering above would buy nothing.
RUN dotnet publish Vortex.Main/Vortex.Main.csproj \
        --configuration Release \
        --no-restore \
        --output /app/publish

# =================================================================================================
# Runtime stage
# =================================================================================================
# Vortex.Main is a console host (OutputType Exe), but Vortex.WebApi and Vortex.Dashboard.API each
# start their own Kestrel WebApplication and reference the Microsoft.AspNetCore.App shared
# framework. The plain `runtime` image does not carry it, so the host would die on the first
# WebApplication build — `aspnet` is the correct base here, not `runtime`.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# The published output, owned by the non-root `app` user that the official .NET images provide
# (uid 1654). Copying with --chown avoids a second full-size layer just to fix ownership.
COPY --from=build --chown=app:app /app/publish ./

# Writable directories the host expects under its content root, created while still root:
#   logs/    — Vortex:Observability:AuditDeadLetterPath defaults to logs/audit-dead-letter.jsonl
#   plugins/ — the plugin loader probes AppContext.BaseDirectory/plugins
#   assets/  — Vortex:Observability:AssetsLocalRoot defaults to ./assets
RUN mkdir -p /app/logs /app/plugins /app/assets \
    && chown -R app:app /app/logs /app/plugins /app/assets

USER app

# Documentation only — publishing these is docker-compose.yml's job.
#   30000 game TCP socket, 30001 game WebSocket socket, 8080 web API, 9000 operator dashboard.
# The Orleans silo (11111) and gateway (3000) ports are deliberately absent: this is a single
# in-process silo and nothing outside the container should reach them.
EXPOSE 30000 30001 8080 9000

ENTRYPOINT ["dotnet", "Vortex.Main.dll"]
