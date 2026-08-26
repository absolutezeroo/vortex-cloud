# Endpoint file index

> **Generated reference index.** This file inventories code symbols from a static scan of the
> repository at commit `e57f0be79a96` and should not be used as the sole source for runtime semantics.
> Regenerate with `/document-vortex update`. Explanatory pages live one directory up.


Files that register HTTP endpoints, detected by any of `MapGet` / `MapPost` / `MapPut` / `MapDelete` / `MapPatch` / `MapReadGet` / `MapGroup` &mdash; the repo-local `MapReadGet` wrapper is why a plain `MapGet` scan under-reports. Route-level detail, required capability and live-state dependency live in [`../08-dashboard/capabilities.md`](../08-dashboard/capabilities.md) and [`../08-dashboard/operations.md`](../08-dashboard/operations.md); the authoritative route list is the test-enforced `Vortex.Dashboard.Tests/Hosting/authorization-matrix.txt`.

| File | Host project |
|---|---|
| `Vortex.Dashboard.API/Hosting/DashboardEndpoints.Account.cs` | `Vortex.Dashboard.API` |
| `Vortex.Dashboard.API/Hosting/DashboardEndpoints.Achievements.cs` | `Vortex.Dashboard.API` |
| `Vortex.Dashboard.API/Hosting/DashboardEndpoints.Audit.cs` | `Vortex.Dashboard.API` |
| `Vortex.Dashboard.API/Hosting/DashboardEndpoints.Backup.cs` | `Vortex.Dashboard.API` |
| `Vortex.Dashboard.API/Hosting/DashboardEndpoints.Benchmark.cs` | `Vortex.Dashboard.API` |
| `Vortex.Dashboard.API/Hosting/DashboardEndpoints.Bots.cs` | `Vortex.Dashboard.API` |
| `Vortex.Dashboard.API/Hosting/DashboardEndpoints.Catalog.cs` | `Vortex.Dashboard.API` |
| `Vortex.Dashboard.API/Hosting/DashboardEndpoints.Chatlogs.cs` | `Vortex.Dashboard.API` |
| `Vortex.Dashboard.API/Hosting/DashboardEndpoints.Config.cs` | `Vortex.Dashboard.API` |
| `Vortex.Dashboard.API/Hosting/DashboardEndpoints.Console.cs` | `Vortex.Dashboard.API` |
| `Vortex.Dashboard.API/Hosting/DashboardEndpoints.Content.cs` | `Vortex.Dashboard.API` |
| `Vortex.Dashboard.API/Hosting/DashboardEndpoints.Directory.cs` | `Vortex.Dashboard.API` |
| `Vortex.Dashboard.API/Hosting/DashboardEndpoints.Economy.cs` | `Vortex.Dashboard.API` |
| `Vortex.Dashboard.API/Hosting/DashboardEndpoints.Furniture.cs` | `Vortex.Dashboard.API` |
| `Vortex.Dashboard.API/Hosting/DashboardEndpoints.Insights.cs` | `Vortex.Dashboard.API` |
| `Vortex.Dashboard.API/Hosting/DashboardEndpoints.Moderation.cs` | `Vortex.Dashboard.API` |
| `Vortex.Dashboard.API/Hosting/DashboardEndpoints.Monitoring.cs` | `Vortex.Dashboard.API` |
| `Vortex.Dashboard.API/Hosting/DashboardEndpoints.MysteryBox.cs` | `Vortex.Dashboard.API` |
| `Vortex.Dashboard.API/Hosting/DashboardEndpoints.Navigator.cs` | `Vortex.Dashboard.API` |
| `Vortex.Dashboard.API/Hosting/DashboardEndpoints.Polls.cs` | `Vortex.Dashboard.API` |
| `Vortex.Dashboard.API/Hosting/DashboardEndpoints.Prizes.cs` | `Vortex.Dashboard.API` |
| `Vortex.Dashboard.API/Hosting/DashboardEndpoints.QuestContent.cs` | `Vortex.Dashboard.API` |
| `Vortex.Dashboard.API/Hosting/DashboardEndpoints.Quests.cs` | `Vortex.Dashboard.API` |
| `Vortex.Dashboard.API/Hosting/DashboardEndpoints.Rooms.cs` | `Vortex.Dashboard.API` |
| `Vortex.Dashboard.API/Hosting/DashboardEndpoints.Staff.cs` | `Vortex.Dashboard.API` |
| `Vortex.Dashboard.API/Hosting/DashboardEndpoints.Stats.cs` | `Vortex.Dashboard.API` |
| `Vortex.Dashboard.API/Hosting/DashboardEndpoints.TargetedOffers.cs` | `Vortex.Dashboard.API` |
| `Vortex.Dashboard.API/Hosting/DashboardEndpoints.cs` | `Vortex.Dashboard.API` |
| `Vortex.Dashboard.Tests/Hosting/DashboardControlPlaneMetricsTests.cs` | `Vortex.Dashboard.Tests` |
| `Vortex.Dashboard.Tests/Hosting/DashboardStepUpTests.cs` | `Vortex.Dashboard.Tests` |
| `Vortex.Supervisor/Endpoints/SupervisorEndpoints.cs` | `Vortex.Supervisor` |
| `Vortex.WebApi/Hosting/WebApiEndpoints.cs` | `Vortex.WebApi` |
