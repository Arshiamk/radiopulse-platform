# Technical Decisions

## 2026-02-15

1. The repository was initialized from an empty git root, so all phases are scaffolded from scratch.
2. `.NET SDK 10.0.103` is pinned in `global.json` to keep builds reproducible.
3. `LangVersion` is set to `preview` to align with C# 14.
4. Warnings are treated as errors to enforce quality from the first phase.
5. PHASE 0 includes governance and hygiene artifacts up front:
   - `LICENSE` (MIT)
   - `.github/CODEOWNERS`
   - `CONTRIBUTING.md`
   - `SECURITY.md`
   - GitHub issue templates
6. PHASE 1 uses .NET Aspire AppHost for orchestration and ServiceDefaults for shared telemetry, health, and service discovery defaults.
7. API endpoints are namespaced under `/api/*` from the start to keep compatibility with upcoming versions.
8. Blazor Web includes an explicit phase-1 auth placeholder page (`/auth`) prior to JWT integration in Phase 5.
9. PHASE 2 introduces Postgres and Redis as Aspire resources, with EF Core 10 + Npgsql 10 for persistence.
10. API applies migrations at startup and seeds baseline station/show/user/poll data for local demos.
11. SignalR hub endpoint is `/hubs/engagement` and is used by Blazor Server for cross-tab live poll/shoutout updates.
12. PHASE 4 media UX is centered on `/api/now-playing`, `/api/shows`, and `/api/episodes` to support both Web and MAUI clients.
13. PHASE 5 uses JWT bearer auth with a local dev token endpoint, auth-protected write operations, and fixed-window rate limiting on public reads.
14. Worker supports Azure-backed transcript/summary generation when `UseAzureAi=true` or when Azure env vars are present; otherwise it defaults to deterministic `FakeAzureAiProvider` locally.
15. Recommendations use an ML.NET regression model persisted to `models/recommendations.zip` and trained automatically on startup when absent.
16. MAUI app is implemented and build-targeted to Windows (`net10.0-windows10.0.19041.0`) in this environment to keep CI builds deterministic without Android SDK setup.
17. Containerization is standardized with per-service Dockerfiles plus `docker-compose.yml`, and deployment manifests use a minimal Kustomize base under `k8s/base`.
18. API integration tests use `WebApplicationFactory` with `UseInMemoryDb=true` to keep fast feedback without external infra in CI.
