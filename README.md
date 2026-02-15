# RadioPulse

Europe-wide commercial radio engagement platform built on `.NET 10` + `Aspire`.

## Progress
- [x] Phase 0 - Repo scaffolding and standards
- [x] Phase 1 - Aspire orchestration and core services
- [x] Phase 2 - Data layer and domain
- [x] Phase 3 - Real-time engagement
- [x] Phase 4 - Now playing, schedule, podcast UX
- [x] Phase 5 - AuthN/AuthZ and security hardening
- [x] Phase 6 - Azure AI integration with local stub
- [x] Phase 7 - ML.NET recommendations
- [x] Phase 8 - .NET MAUI app
- [x] Phase 9 - Docker, K8s, CI/CD
- [x] Phase 10 - Polish and demo assets

## Architecture
```text
                +------------------------------+
                |    RadioPulse.AppHost        |
                |  (.NET Aspire orchestrator)  |
                +---------------+--------------+
                                |
      +-------------------------+-------------------------+
      |                         |                         |
+-----v------+          +-------v-------+         +------v------+
| RadioPulse |          | RadioPulse    |         | RadioPulse  |
| .Web       |<--HTTP-->| .Api          |<--EF10->| PostgreSQL  |
| Blazor SSR |          | Minimal API   |         | (Aspire)    |
+-----+------+          | SignalR Hub   |         +-------------+
      |                 +-------+-------+
      |                         |
      |                         +-----> Redis
      |                                (Aspire)
      |
+-----v-------+        +---------------------------+
| RadioPulse  |        | RadioPulse.Worker         |
| .Mobile     |<------>| transcript + summary flow |
| .NET MAUI   |        | FakeAzure/Azure provider  |
+-------------+        +-------------+-------------+
                                      |
                                      +----> Transcripts persisted

+-------------------+
| RadioPulse.Ml     |
| ML.NET train/pred |
+-------------------+
```

## Screenshots (placeholders)
- `docs/screenshots/web-engagement.png`
- `docs/screenshots/web-media.png`
- `docs/screenshots/web-recommendations.png`
- `docs/screenshots/mobile-nowplaying.png`
- `docs/screenshots/mobile-vote.png`

## Core Endpoints
- `GET /api/status`
- `GET /api/stations`
- `GET /api/shows`
- `GET /api/episodes`
- `GET /api/now-playing`
- `GET /api/polls/active`
- `POST /api/polls` (auth)
- `POST /api/polls/votes` (auth)
- `GET /api/shoutouts`
- `POST /api/shoutouts` (auth)
- `GET /api/transcripts/top-moments`
- `GET /api/transcripts/search?term=...`
- `GET /api/recommendations/{userId}`
- `GET /api/auth/dev-token/{userId}`
- `Hub /hubs/engagement` (auth)

## How To Run Locally (Aspire)
1. Install `.NET SDK 10.0.103`.
2. If you see `UntrustedRoot` for localhost HTTPS:
   - close all browser windows
   - run `scripts/devcert.ps1`
   - restart AppHost
3. Start stack:
   - `dotnet run --project src/RadioPulse.AppHost/RadioPulse.AppHost.csproj`
4. Open Aspire dashboard and launch `web`.
5. Validate:
   - `dotnet build src/RadioPulse.slnx -c Debug`
   - `dotnet test src/RadioPulse.slnx -c Debug`
6. Demo flow in web:
   - open `/weather` to verify API probe
   - open `/diagnostics` and run checks (all should pass)
   - open `/auth` and sign in as `Lena` or `Luna`
   - open `/engagement` to create polls/votes/shoutouts
   - open `/media` and `/recommendations` to verify content and signed-in personalization
7. Optional automated smoke validation:
   - `powershell -ExecutionPolicy Bypass -File .\scripts\smoke.ps1 -Configuration Debug`

## How To Run With Docker
1. Build and start:
   - `docker compose up -d --build`
2. Open:
   - Web: `http://localhost:8081`
   - API: `http://localhost:8080/api/status`
3. Notes:
   - Containers run HTTP-only in local compose (`EnableHttpsRedirection=false`).
   - Web API discovery is wired via `services__api__http__0=http://api:8080`.
4. Stop:
   - `docker compose down -v`

## How To Deploy To K8s
1. Build/push images to GHCR (`ghcr.io/<owner>/radiopulse-{api,web,worker}:latest`).
2. Update image names in `k8s/base/*.yaml`.
3. Apply manifests:
   - `kubectl apply -k k8s/base`
4. Optional local ingress host: map `radiopulse.local` to ingress controller.
5. Notes:
   - Web resolves API through `services__api__http__0=http://api:8080`.
   - Liveness/readiness probes are configured for API/Web (`/health`).

## Observability
- Service defaults include health checks (`/health`, `/alive` in dev) and OpenTelemetry.
- Aspire dashboard gives traces/logs/resource graph locally.
- Configure OTLP exporter with `OTEL_EXPORTER_OTLP_ENDPOINT`.

## Security
- JWT bearer auth for write operations + SignalR hub auth.
- Write operations enforce authenticated `userId` (token identity must match payload).
- Local dev token endpoint for demo sessions.
- Public read endpoint rate limiting enabled.
- Input validation and structured error responses included.

## CI/CD
- `ci.yml`:
  - restore/build/test
  - web + api integration tests
  - format check
  - docker build validation
- `cd.yml` (main):
  - release validation (restore/build/test + smoke)
  - buildx + push images to GHCR

## MAUI Notes
- `src/RadioPulse.Mobile` includes Login, Now Playing, Vote/Shoutout, Recommendations pages.
- Current repo build targets Windows MAUI in this environment.
- API URL is configurable via `RADIOPULSE_API_URL` env var and from the Login page input.
- Default mobile API URL is `http://localhost:5003` for local Aspire HTTP profile.
- Windows manifest metadata uses concrete RadioPulse identity/logos (no template placeholders).

## Demo Script
- `docs/demo-script.md`

## Repository Layout
- `src/RadioPulse.AppHost`
- `src/RadioPulse.ServiceDefaults`
- `src/RadioPulse.Api`
- `src/RadioPulse.Web`
- `src/RadioPulse.Web.Tests`
- `src/RadioPulse.Worker`
- `src/RadioPulse.Ml`
- `src/RadioPulse.Mobile`
- `k8s/base`
- `.github/workflows`
- `docs/decisions.md`
