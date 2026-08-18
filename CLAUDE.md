# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

**Блізка (Blizka)** — backend for a Telegram Mini App dating product. .NET 10 / ASP.NET Core Web API, PostgreSQL 16 + PostGIS. No frontend code lives here.

`decomposition.md` at the repo root is the authoritative task breakdown (epics T-0.x … T-21.x) derived from the interface spec (30 screens) and a backend spec. It defines the MVP scope, per-task inputs/outputs/dependencies, and the build order. Read the relevant task section there before implementing a feature — endpoint shapes, business rules, and thresholds (e.g. spark amounts, completeness percentages, undo limits) are specified there, not invented ad hoc.

**The backend spec itself (`backend-spec.md`, referenced by section number throughout `decomposition.md`) does not exist in this repo.** `decomposition.md` is the only spec document actually present. Where a task references a spec section for details it doesn't itself spell out (exact entity fields, enum value sets, etc.), those details don't exist anywhere — they were inferred/decided when the task was implemented, and are called out as such in that task's "Что сделано" notes. Check there before assuming a field or enum value is spec-mandated rather than a judgment call.

## Commands

```bash
# restore / build / test the whole solution
dotnet restore
dotnet build
dotnet test

# run the API (from repo root or from src/Blizka.Host)
dotnet run --project src/Blizka.Host

# run a single test
dotnet test tests/Blizka.UnitTests --filter "FullyQualifiedName~ClassName.MethodName"

# local Postgres (PostGIS-enabled)
docker compose up -d postgres

# EF Core migrations — DbContext lives in Blizka.Data, startup project is Blizka.Host
# (authoring a new migration needs the .NET SDK on the host; there's no container for this step)
dotnet ef migrations add <Name> --project src/Blizka.Data --startup-project src/Blizka.Host

# apply pending migrations — either from the host directly:
dotnet ef database update --project src/Blizka.Data --startup-project src/Blizka.Host
# ...or via the containerized migrator (no local dotnet-ef tool needed, runs against the
# `postgres` compose service): builds Dockerfile.migrator, applies, then exits.
docker compose up --build migrator
```

There is no separate lint/format step configured yet.

## Architecture

Four-layer solution, dependencies point inward toward `App`:

```
Blizka.Host  →  Blizka.Api, Blizka.App, Blizka.Data
Blizka.Api   →  Blizka.App
Blizka.Data  →  Blizka.App
Blizka.App   →  (nothing — pure domain/application core)
```

- **Blizka.App** — domain entities (`Domain/Entities`), enums (`Domain/Enums`), interfaces, MediatR use-case handlers, FluentValidation validators. No ASP.NET Core or EF Core references. It does reference `NetTopologySuite` (plain geometry library, not EF/ASP.NET) because `User.Coordinates`/`City.Coordinates` are typed `Point`/`Point?` directly.
- **Blizka.Data** — EF Core `BlizkaDbContext` (Npgsql + PostGIS via `UseNetTopologySuite`), entity type configurations in `Configurations/` (one `IEntityTypeConfiguration<T>` per entity, registered through `modelBuilder.ApplyConfigurationsFromAssembly`), reference/catalog seed data in `Seed/` (interests, cities, date preferences — wired into configs via `HasData` with deterministic literal GUIDs), repository implementations. `BlizkaDbContextFactory` is the design-time factory `dotnet ef` uses; it does not read Host configuration — it reads the connection string from the `BLIZKA_DB_CONNECTION` env var (falling back to the same local-dev default baked into `appsettings.yaml`), so keep that fallback in sync with `docker-compose.yml` manually if either changes. `Dockerfile.migrator` sets `BLIZKA_DB_CONNECTION` to point at the `postgres` compose service (`Host=postgres`, not `localhost`) — that's how the containerized `migrator` service (see below) reaches the DB without touching app config.
- **Blizka.Api** — a class library (not an executable) containing MVC controllers and request/response DTOs. It carries `<FrameworkReference Include="Microsoft.AspNetCore.App" />` so it can use ASP.NET Core types without being the host. Controllers are wired into the running app via `AddApiLayer()` → `AddApplicationPart`, not by being in the startup project.
- **Blizka.Host** — the actual executable (`Microsoft.NET.Sdk.Web`). `Program.cs` is the composition root: loads YAML config, configures Serilog, CORS, Quartz hosting, and calls each layer's `AddXLayer()` extension method. Has a trailing `public partial class Program;` so `WebApplicationFactory<Program>` works from `Blizka.IntegrationTests`.
- **Blizka.UnitTests** — references `App` + `Data` only; no host, no HTTP.
- **Blizka.IntegrationTests** — references `Host` (transitively pulls in `Api`/`App`/`Data`) for full-pipeline tests via `WebApplicationFactory<Program>`.

Each layer exposes its DI wiring as an `IServiceCollection` extension method (`AddAppLayer`, `AddDataLayer`, `AddApiLayer`) rather than registering services directly in `Program.cs`. Follow that pattern when adding new cross-cutting registrations.

### Configuration

App settings are **YAML, not JSON** (`appsettings.yaml` / `appsettings.Development.yaml` in `Blizka.Host`), loaded via `NetEscapades.Configuration.Yaml`. Both files are explicit `<Content>` items in `Blizka.Host.csproj` with `CopyToOutputDirectory` — a new environment-specific file needs the same treatment or it won't reach the output directory. Config sections in place: `Database`, `Telegram`, `Storage`, `Ai`, `Cors`, `Serilog`, `Logging`. There is deliberately no `Redis` section — Redis was considered for T-0.1 and explicitly deferred; don't add it back without a concrete task that needs it.

### Package versions

Central Package Management is on (`Directory.Packages.props` at the repo root, `ManagePackageVersionsCentrally=true`). `csproj` files must not carry a `Version` attribute on `PackageReference`. To add a new package: temporarily flip `ManagePackageVersionsCentrally` to `false`, run `dotnet add package <Name>` on the target project so NuGet resolves a real net10.0-compatible version, move the resulting `Version` into `Directory.Packages.props` as a `PackageVersion`, strip the inline version from the `csproj`, then flip CPM back to `true` and `dotnet restore` to confirm. Don't hand-guess version numbers — NuGet is the source of truth for what's actually compatible with net10.0.

`Directory.Build.props` sets the shared `TargetFramework` (net10.0), `Nullable`, `ImplicitUsings`, etc. for every project — don't redeclare those per-project.

### Notes on deliberate omissions

- **FluentAssertions is not used** — v8+ requires a paid commercial license above a revenue threshold; avoided to keep the template unencumbered. Use plain `xunit` `Assert`, or raise adding `Shouldly` (MIT) if fluent assertions are wanted later.
- No background jobs are registered yet — `AddQuartz()`/`AddQuartzHostedService()` are wired in `Blizka.Host` but the job list is empty until a task requires one (e.g. `ArchiveStaleMatches`, `CityOpenCheck` from `decomposition.md`). Hangfire was considered and rejected in favor of Quartz.
- `Blizka.Data`'s `BlizkaDbContext` now has the full T-0.2 domain model (20 `DbSet`s — all MVP and Post-MVP entities named in that task) and an `InitialCreate` migration. Tables for entities owned by *other* tasks — `OnboardingDraft`, `UserConsent`, `UserFilter`, `PrivacySettings`, `UserBlock`, `Referral`, `Notification`, and the dilemma catalog behind `Minigame` — don't exist yet; they land with T-2.1/T-2.2/T-5.4/T-16.1/T-16.2/T-20.1/T-14.1 respectively. Don't assume a table exists just because a later task references it in prose.
- `docker compose up`/`docker-compose.yml` only ever runs Postgres and the one-shot `migrator` — there's no `Blizka.Host` service/Dockerfile in compose. The app itself still runs on the host via `dotnet run`; containerizing the API wasn't part of any task yet.
