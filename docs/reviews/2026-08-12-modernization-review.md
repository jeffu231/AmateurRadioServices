# Amateur Radio Services modernization review

- **Review date:** 2026-08-12
- **Scope:** code quality, correctness, performance, security, architecture, API design, testing, CI/CD, and deployment
- **Reviewed baseline:** one .NET 10 ASP.NET Core Web API project, six controllers, two external-service clients, and no test project

## Executive summary

The project is small, understandable, and already uses several sound foundations: .NET 10 is pinned, nullable reference types are enabled, outbound HTTP clients come from `IHttpClientFactory`, network calls are asynchronous, JSON logging and Problem Details are registered, and API versioning/OpenAPI are present. A wholesale rewrite or a multi-project clean-architecture conversion would add cost without addressing the highest risks.

Modernization should first make the existing service safe and predictable. The highest-priority concerns are:

1. QRZ XML responses are written to debug logs, and the upstream QRZ model—including session fields—is returned directly from public endpoints. This can disclose a paid-service session token and personal data.
2. The application is an unauthenticated proxy to credentialed, quota-limited upstream services and has no rate limiting. If exposed publicly, it can be abused using the operator's QRZ/APRS credentials.
3. QRZ session state is held in unsynchronized static fields while `QrzDataService` instances are transient typed clients. Concurrent requests can stampede session creation, overwrite shared state, or make calls with a stale/empty token.
4. API failure semantics are inconsistent. Several upstream failures are converted to DTOs and returned as HTTP 200; exception messages can reach clients; malformed grids can become HTTP 500 responses.
5. The APRS grid endpoint has a confirmed multi-result correctness defect: every output grid is calculated from `record.Entries[0]`, not the entry being projected.
6. The build reports a known high-severity vulnerability in transitive package `Microsoft.OpenApi` 2.4.1 ([GHSA-v5pm-xwqc-g5wc](https://github.com/advisories/GHSA-v5pm-xwqc-g5wc)).
7. There are no automated tests, and the GitHub workflow publishes from `master` without running an explicit build or test and without pull-request validation.

The recommended target is a pragmatic modular monolith: retain one deployable API, keep controllers thin, introduce explicit provider interfaces and public contracts, isolate QRZ session management, validate options and requests, and add an integration-test project. Split production code into additional assemblies only if the application grows enough to justify the boundary.

## Current-state assessment

### Strengths to preserve

- `global.json` pins SDK 10.0.103 and allows feature-band roll-forward.
- `CoreServices.csproj` enables nullable reference types and implicit usings.
- Typed HTTP clients avoid manually constructing `HttpClient` per request.
- No blocking `.Result`, `.Wait()`, `async void`, or fire-and-forget tasks were found.
- `JsonSerializerOptions` and `XmlSerializer` are reused rather than rebuilt per call.
- Query parameters are encoded with `QueryHelpers.AddQueryString`.
- API versioning, Swagger/OpenAPI, JSON logging, Problem Details, and a multi-stage Dockerfile already exist.
- The repository is compact enough to modernize incrementally with low migration risk.

### Overall risk

| Area | Rating | Summary |
|---|---:|---|
| Security/privacy | High | Raw QRZ payload logging, possible session-token exposure, public use of paid upstream credentials, no rate limiting |
| Correctness | High | APRS projection defect, fragile QRZ/session handling, incorrect error text, invalid input can produce 500 |
| Reliability | High | No cancellation, explicit timeout/resilience policy, synchronized session refresh, or consistent upstream failure mapping |
| Testability | High | Concrete service dependencies, third-party transport models exposed as API models, no tests |
| Performance | Medium | External I/O dominates; session stampede and repeated lookups matter more than micro-optimizations |
| Maintainability | Medium | Business rules in controllers, configuration via string keys, inconsistent contracts/documentation |
| Delivery | High | No PR checks or explicit CI build/test; vulnerable dependency is not a gate |
| Deployment | Medium | Container runs without explicit non-root hardening or health checks; Swagger is always exposed |

## Prioritized findings

### P0 — address before expanding public use

#### SEC-01: QRZ secrets and payload data can leak through logs and API contracts

**Evidence**

- `QrzDataService.CreateSessionAsync` logs the complete XML authentication response at `CoreServices/Services/QrzDataService.cs:51-52`.
- `QueryCallDataAsync` logs every complete callsign XML response at `CoreServices/Services/QrzDataService.cs:172-174`.
- `CallsignController` returns `QRZDatabase` directly at `CoreServices/Controllers/CallsignController.cs:50-56`.
- The serialized upstream model includes `QRZDatabaseSession.Key` and many address/contact fields in `CoreServices/Model/Qrz/QRZDatabase.cs`.
- `QRZDatabaseCallsign.ToString()` includes address and email fields, making accidental PII logging easy.

**Impact**

Debug logging can persist an active QRZ session key and personal lookup data. Returning the upstream session object creates a second path by which the key may be disclosed. It also makes the public contract inseparable from the vendor's XML schema.

**Recommendation**

- Remove raw XML logging. Log only event name, upstream status, duration, callsign hash or carefully reviewed identifier, and a vendor error code.
- Introduce public response DTOs that contain only fields intentionally supported by this API. Never serialize the QRZ `Session` object or key.
- Keep vendor XML classes internal to the QRZ infrastructure boundary. Prefer generated transport classes plus an explicit mapper; do not hand-modernize generated XML types.
- Ensure production logging never records passwords, API keys, session tokens, full query strings containing the APRS key, full upstream payloads, email, or street address unless those fields are explicitly required and governed.
- Review retained logs and rotate/revoke QRZ sessions if raw response logging has been enabled in a shared environment.

#### SEC-02: unauthenticated callers can consume paid upstream services

**Evidence**

- No authentication scheme, authorization policy, `[Authorize]` attribute, or rate limiter is configured in `Program.cs`.
- All controllers expose credential-backed QRZ/APRS operations publicly.
- Docker Compose advertises the API through Traefik.

**Impact**

If the deployed route is internet-accessible, any caller can consume upstream subscription quota, amplify traffic, or trigger concurrent QRZ authentication attempts using the owner's credentials.

**Recommendation**

- Decide and document whether the API is private, authenticated, or intentionally public.
- For a private service, require an API key, mTLS, or identity-provider authentication and apply authorization globally by default.
- For an intentionally public service, add ASP.NET Core rate limiting with per-client and global concurrency limits, response caching where freshness allows, upstream quota monitoring, and explicit abuse controls.
- Return 429 with `Retry-After` when local capacity or quota protection is engaged.

#### DEP-01: audited high-severity dependency vulnerability

**Evidence**

`dotnet build` and `dotnet list package --include-transitive` report `NU1903` for transitive `Microsoft.OpenApi` 2.4.1 and GHSA-v5pm-xwqc-g5wc. It is brought in through the current OpenAPI/Swagger dependency graph.

**Recommendation**

- Upgrade the owning top-level package to a version that resolves a patched `Microsoft.OpenApi` version, then rerun build, API document generation, and integration tests.
- Use a direct version override only after confirming compatibility with the owning package.
- Add `dotnet list package --vulnerable --include-transitive` or NuGet audit enforcement to pull-request CI and fail on high/critical findings.

### P1 — correctness and reliability baseline

#### COR-01: APRS grid results use the wrong coordinates

**Evidence**

Within the loop over `record.Entries`, `AprsController.GetGrid` calculates each grid using `record.Entries[0].Lat` and `.Lng` at `CoreServices/Controllers/AprsController.cs:102-106`.

**Impact**

For a request containing multiple APRS identifiers, every station receives the first station's grid.

**Recommendation**

Calculate from `recordEntry.Lat` and `recordEntry.Lng`. Add a regression test with at least two entries in different Maidenhead grids.

#### COR-02: upstream errors are often returned as successful HTTP responses

**Evidence**

- `AprsService` catches every exception and creates a failure record containing `ex.Message` at `CoreServices/Services/AprsService.cs:44-69`; the base APRS endpoint returns any non-null record as 200 at `CoreServices/Controllers/AprsController.cs:36-44`.
- `QrzDataService` converts transport/deserialization/authentication failures into `QRZDatabase` session values at `CoreServices/Services/QrzDataService.cs:79-85` and `182-188`.
- `CallsignController` returns every such value as 200 at `CoreServices/Controllers/CallsignController.cs:50-56`.

**Impact**

Clients cannot distinguish success from dependency failure using HTTP semantics. Monitoring sees false successes, caches may retain failures, and exception details can be disclosed.

**Recommendation**

- Use a typed internal result or a small exception hierarchy to distinguish validation failure, not found, upstream authentication/subscription failure, timeout, unavailable service, invalid payload, and unexpected defects.
- Map these centrally to `ValidationProblemDetails`, 404, 429, 502, 503, or 504 as appropriate. Do not return raw exception messages.
- Reserve HTTP 403 for authorization of the current caller. An expired operator-owned QRZ subscription is normally a dependency/configuration failure (typically 503), not caller authorization failure.
- Return typed `ActionResult<T>` responses and make OpenAPI response declarations match actual behavior.

#### COR-03: contact enhancement has fragile null/error behavior

**Evidence**

- `callInfo.Session.Select(x => x.Error).ToString()` at `CoreServices/Controllers/ContactController.cs:68` returns the enumerable type name rather than joined error messages.
- `callInfo.Callsign.Length` at line 71 can dereference `null` when an upstream payload has a session but no callsign array.
- Grid conversion at lines 95-99 is not protected by request validation; invalid locators can escape as 500 responses.
- Grid matching uses the full caller-provided `DxGrid`, while the XML documentation describes comparison of the first four characters.

**Recommendation**

- Move enhancement into `IContactEnhancer`; keep the controller responsible only for HTTP validation and result mapping.
- Normalize and validate callsigns and Maidenhead locators before calling the domain calculation.
- Use null-safe pattern matching for vendor arrays and join sanitized error codes/messages explicitly.
- Specify the exact four-/six-/eight-character grid policy and compare normalized values using ordinal semantics.
- Return a new response value rather than mutating the input model.

#### REL-01: QRZ session state is globally shared but not synchronized

**Evidence**

- `_sessionToken` and `_subExpirationTime` are static mutable fields at `CoreServices/Services/QrzDataService.cs:13-14`.
- `AddHttpClient<QrzDataService>()` registers the typed client as transient in `Program.cs:34-35`.
- `ValidateSessionAsync` and retry paths can concurrently call `CreateSessionAsync`, clear the token, and overwrite shared fields without a lock.
- The refresh retry ignores whether `CreateSessionAsync` succeeded at `CoreServices/Services/QrzDataService.cs:155-157`.

**Impact**

A burst against a cold or expired session can create multiple sessions, race token writes, waste quota, and issue lookups with an invalid token. Static state also leaks across service instances/tests and is incompatible with future multi-account configuration.

**Recommendation**

- Introduce an `IQrzSessionProvider` with one well-defined lifetime and encapsulated token/expiration state.
- Protect refresh with `SemaphoreSlim` plus double-checked validation so only one caller authenticates while other callers await it.
- Represent expiration as `DateTimeOffset` with an explicit time-zone policy and refresh shortly before expiry.
- Propagate authentication failure rather than querying with an empty token.
- Keep multi-instance deployment in mind: either accept one session per process, or use a distributed coordination/cache strategy if QRZ requires a single account session across replicas.

#### REL-02: outbound HTTP has no end-to-end cancellation or explicit resilience policy

**Evidence**

- Controller actions and service methods accept no `CancellationToken`.
- `GetAsync`, `PostAsync`, and response-content reads use defaults.
- HTTP status is not checked before QRZ response deserialization.
- `HttpResponseMessage`, response streams, and form content are not consistently disposed.

**Recommendation**

- Accept `CancellationToken` in controller/application/provider methods and pass it through every HTTP and serialization call.
- Configure named/typed clients centrally with base address, headers, a bounded timeout, and `Microsoft.Extensions.Http.Resilience` policies.
- Retry only transient and safe operations; use jitter and honor `Retry-After`. Treat QRZ authentication POST retries deliberately to avoid session churn.
- Request `HttpCompletionOption.ResponseHeadersRead`, dispose responses/streams, call or explicitly map non-success statuses, and deserialize asynchronously from the stream.
- Put response-size limits around untrusted upstream content.

#### API-01: request validation and documented contracts are incomplete

**Evidence**

- Callsigns are checked only for empty strings; lengths and allowed characters/count are unbounded.
- Latitude/longitude ranges and Maidenhead locator syntax are not validated.
- `ConfigurationController` declares a `string` response but returns an anonymous `{ applicationVersion }` object at `CoreServices/Controllers/ConfigurationController.cs:19-24`.
- Several endpoints declare 400 but return 404/403/500-family responses not described in OpenAPI.
- API versioning is registered twice at `CoreServices/Program.cs:57-78`. The route requires a URL version segment while a header reader is also enabled, so the intended version policy is unclear.

**Recommendation**

- Add request DTO validation using data annotations or a dedicated validator and return standard validation problems.
- Define a `VersionResponse` DTO and public DTOs for every stable response shape.
- Choose a single clear version-reading policy. If URL-segment versioning remains, remove the header reader and redundant registration unless dual-input behavior is explicitly required and tested.
- Prefer lowercase route templates and consistent resource-oriented action names for the next API version; preserve v1 compatibility while migrating.
- Remove XML output formatters if XML is not a supported public representation. Most actions currently restrict output to JSON, so the configured XML formatter adds ambiguity.

### P2 — architecture and maintainability

#### ARCH-01: controllers contain application and vendor-specific business rules

`ContactController` knows QRZ session layout, parses subscription dates, interprets vendor errors, selects grids, and calculates a bearing. APRS projection also lives in its controller. These responsibilities make behavior hard to unit test and couple HTTP endpoints to transport formats.

**Recommended dependency direction**

```text
HTTP controllers
    -> application use cases (contact enhancement, location queries)
        -> provider interfaces (IAprsClient, IQrzClient, IQrzSessionProvider)
            -> typed HttpClient adapters and vendor transport DTOs

HTTP response DTOs <- explicit mapping <- application/domain results
```

Start with folders/namespaces inside the existing project:

- `Contracts`: stable request/response records exposed over HTTP.
- `Application`: `IContactEnhancer` and orchestration/result types.
- `Integrations/Aprs`: options, vendor DTOs, client implementation.
- `Integrations/Qrz`: options, generated XML DTOs, session provider, client implementation.
- `Domain` only for genuinely reusable radio concepts/calculations.

Do not introduce repositories: there is no persistence boundary. Do not introduce generic command-handler infrastructure for six conventional HTTP endpoints unless cross-cutting use-case dispatch becomes a real need. `IHttpClientFactory` already supplies the relevant factory behavior. Introduce Strategy only when a second callsign/APRS provider is actually supported. This keeps the design aligned with SOLID without creating ceremonial layers.

#### ARCH-02: configuration is stringly typed and does not fail fast

`AprsService` and `QrzDataService` read flat configuration keys and silently replace missing credentials with empty strings. The service therefore starts successfully and fails only during live traffic.

**Recommendation**

- Add `AprsOptions` and `QrzOptions` sections with required credentials, validated base URI, agent identifier, timeout, and optional cache/rate settings.
- Register with `AddOptions<T>().BindConfiguration(...).ValidateDataAnnotations().ValidateOnStart()`.
- Store production secrets in the deployment platform's secret mechanism. Environment-variable injection is acceptable, but prefer hierarchical names such as `Qrz__Username` and never place real values in JSON or Compose files.
- Keep base addresses and headers in DI registration rather than mutating the client in constructors.

#### MAINT-01: generated and hand-written contracts need clearer boundaries

`QRZDatabase.cs` is a 684-line generated-style file containing multiple public classes, legacy naming, mutable arrays, and nullable mismatches. This is reasonable for XML serialization but unsuitable as the public API or domain model.

**Recommendation**

- Preserve generated types as generated artifacts and exclude them from style/documentation rules where appropriate.
- Keep the source schema and a repeatable generation command in the repository, or replace the generated file with small purpose-built internal XML DTOs if only a subset is required.
- Map to immutable public records. Do not require generated XML DTOs to follow the one-type-per-file or record rules if doing so compromises regeneration/serialization.
- Move `ObsoleteOperationDescriptionFilter` out of `ConfigureSwaggerOptions.cs`; hand-written types should follow one type per file.

#### MAINT-02: documentation and style enforcement are partial

- Public controllers, services, DTOs, and properties lack consistent XML documentation.
- Existing `<param>` and `<returns>` tags are often empty, and Swagger does not include an XML documentation file.
- Constructor style is inconsistent; classes not designed for inheritance are unsealed.
- `MaidenheadController` injects an unused logger.
- Logging includes an invalid/unnamed placeholder (`"Agent = {}"`) and raw message strings instead of consistently named structured properties.

**Recommendation**

- Generate an XML documentation file, resolve/document public API warnings selectively, and configure Swagger to consume it.
- Document public contracts and non-obvious behavior; exclude generated vendor code from the requirement.
- Use primary constructors consistently where they improve readability, seal concrete controllers/services, and depend on interfaces at application boundaries.
- Expand `.editorconfig` with agreed analyzer severities and enforce it in CI. Treat warnings as errors in CI after establishing a clean baseline.
- Use named structured logging properties and source-generated `LoggerMessage` methods for high-volume integration events.
- Use resource files only if the API will localize client-facing messages; localization infrastructure is not a current architectural need.

### P2 — performance and operability

#### PERF-01: optimize upstream behavior before local micro-allocations

The service is network-bound. The important performance risks are duplicate upstream requests, a session-refresh stampede, unbounded caller concurrency, and avoidable buffering. Converting small DTOs to `Span<T>` or broadly using `ValueTask<T>` would add complexity with no demonstrated benefit.

**Recommendation**

- Fix synchronized QRZ session refresh first.
- Add short, bounded caching only where product freshness permits: callsign information may tolerate a longer TTL; APRS positions generally require a short TTL or no cache. Include negative-cache behavior carefully.
- Use request coalescing for identical concurrent lookups to protect vendor quotas.
- Stream-deserialize upstream JSON/XML where practical and cap response sizes.
- Add response compression/output caching only after measuring response size and access patterns.
- Benchmark or load-test before adopting source-generated JSON metadata, `ValueTask`, pooling, `Span<T>`, or parallel processing. These are low-value for the current workload.

#### OPS-01: health, telemetry, and production pipeline hardening are missing

**Recommendation**

- Add separate liveness and readiness endpoints. Liveness must not call paid upstream services; readiness can validate local configuration and expose dependency state without consuming quota.
- Add OpenTelemetry traces/metrics for request duration, upstream duration/status, timeouts, QRZ session refreshes/failures, rate-limit rejections, and cache effectiveness. Do not put callsigns or personal data in high-cardinality labels.
- Place exception handling early in the pipeline and use structured Problem Details customization with a trace identifier.
- If running behind Traefik, configure forwarded headers with trusted proxy/network settings before redirects, authentication, or URL generation.
- Restrict Swagger in production or protect it if the API is private.
- Run the container as the built-in non-root app user, add a health check, set resource limits, and consider a read-only root filesystem.
- Avoid the extra Dockerfile build by publishing with `--no-build` after the explicit build stage, or simplify to a single publish stage.

## Testing strategy

Create `CoreServices.Tests`, add it to `CoreServices.sln`, and use xUnit plus `Microsoft.AspNetCore.Mvc.Testing`. Prefer a small fake `HttpMessageHandler` or local stub server over mocking `HttpClient` itself.

| Test layer | Initial coverage |
|---|---|
| Unit | Callsign normalization and `/R`/`/P` fallback; APRS multi-entry grid projection; contact grid selection; subscription parsing; distance/bearing rounding; result-to-status mapping |
| Provider | QRZ/APRS success, non-success status, malformed payload, timeout, cancellation, response-size limit, session expiry, and retry behavior |
| Concurrency | A burst against an empty/expired QRZ session performs exactly one authentication refresh and all waiters observe a valid result |
| API integration | Model validation, Problem Details, 404/429/502/503/504 contracts, deprecation headers, content negotiation, version selection, and Swagger generation |
| Security | Session keys/passwords/API keys never appear in serialized responses or captured logs; unauthenticated/rate-limited behavior matches policy |
| Container smoke | Non-root startup, health endpoint, configuration failure, graceful shutdown, and forwarded-header behavior |

Each confirmed defect in this review should receive a failing regression test before its fix. For vendor XML, retain minimal sanitized fixtures that cover successful login, successful lookup, missing callsign, expired subscription, invalid session, and malformed XML.

## CI/CD review

The current `.github/workflows/build.yml` runs only on pushes to `master` that change `CoreServices/**`. It does not run on pull requests, and it performs restore/versioning/publish without explicit build or test steps. Changes to `global.json`, `CoreServices.sln`, workflow files, Compose, or shared repository configuration may not trigger it. `versionize` is installed globally without a pinned version, and `ad-m/github-push-action@master` is not pinned to an immutable release/commit.

Recommended pipeline split:

1. **PR validation (read-only permissions):** restore, build Release with analyzers, test with coverage, formatting verification, NuGet vulnerability audit, OpenAPI generation/validation, and optional container build/smoke test.
2. **Release (protected branch/tag):** deterministic versioning, publish container, scan image, generate SBOM/provenance, sign if required, push, and create release.
3. Pin .NET tools in `.config/dotnet-tools.json`, pin actions to vetted versions or commit SHAs, use least-privilege token permissions, and avoid workflows that mutate the protected branch as part of normal publishing.

Suggested validation sequence:

```bash
dotnet restore CoreServices.sln
dotnet build CoreServices.sln -c Release --no-restore -p:TreatWarningsAsErrors=true
dotnet test CoreServices.sln -c Release --no-build --collect:"XPlat Code Coverage"
dotnet format CoreServices.sln --verify-no-changes --no-restore
dotnet list CoreServices/CoreServices.csproj package --vulnerable --include-transitive
dotnet publish CoreServices/CoreServices.csproj -c Release --no-build
```

Use a package lock file if reproducible application restores are a priority. Central package management is optional while there is only one production project.

## Phased modernization plan

### Phase 0: containment and known defects (1–3 days)

- Stop raw QRZ XML/PII/session logging and stop returning upstream session fields.
- Decide access policy; add at least global/per-client rate limiting before public exposure.
- Resolve the `Microsoft.OpenApi` vulnerability.
- Fix APRS multi-entry grid calculation.
- Fix contact error aggregation/null handling and reject invalid grids/coordinates.

**Exit criteria:** secrets/session keys cannot appear in responses or logs; the audited high-severity vulnerability is gone; regression tests cover confirmed defects.

### Phase 1: safety net and reliable contracts (3–6 days)

- Add the xUnit/integration-test project and PR validation workflow.
- Introduce stable public DTOs and centralized Problem Details mapping.
- Add request validation and accurate OpenAPI response declarations.
- Propagate cancellation and configure explicit HTTP timeouts/status handling.
- Consolidate API version registration and content-negotiation policy.

**Exit criteria:** every endpoint has success, validation, dependency-failure, and cancellation coverage; CI blocks regressions and vulnerable dependencies.

### Phase 2: integration architecture (4–8 days)

- Introduce `IAprsClient`, `IQrzClient`, `IQrzSessionProvider`, typed validated options, and explicit transport-to-contract mapping.
- Move contact orchestration out of the controller.
- Synchronize/proactively refresh QRZ sessions and add deliberate resilience policies.
- Add quota-aware caching/request coalescing if usage data justifies it.

**Exit criteria:** controllers depend on application abstractions, vendor DTOs remain internal, concurrent session tests pass, and missing configuration fails at startup.

### Phase 3: production operations (3–6 days)

- Add health endpoints, OpenTelemetry, dashboards/alerts, protected Swagger, and forwarded-header configuration.
- Harden the container for non-root execution and add smoke/security scanning.
- Separate PR validation from release and make releases deterministic.

**Exit criteria:** deployment has actionable health/telemetry, least-privilege pipeline permissions, and a repeatable rollback-ready artifact.

### Phase 4: measure and simplify (ongoing)

- Load-test real traffic patterns and tune timeouts, rate limits, caching, and concurrency.
- Remove obsolete v1 path-based callsign endpoint after the published deprecation window.
- Split production assemblies only when team ownership, independent reuse, or dependency enforcement makes it valuable.

## Validation performed for this review

| Command/check | Result |
|---|---|
| `dotnet build CoreServices.sln --no-restore -p:EnableNETAnalyzers=true -p:AnalysisLevel=latest` | Passed; one `NU1903` high-severity vulnerability warning |
| `dotnet publish CoreServices/CoreServices.csproj -c Release --no-restore` | Passed; same vulnerability warning |
| `dotnet list CoreServices/CoreServices.csproj package --include-transitive` | Confirmed `Microsoft.OpenApi` 2.4.1 as the vulnerable transitive package |
| `dotnet test CoreServices.sln --no-restore` | Exited successfully but discovered no test project/output |
| Static async scan | No blocking waits, `async void`, or manually created `HttpClient` found |
| `dotnet format --verify-no-changes` | Inconclusive in the restricted review environment because Roslyn's build host could not create its named pipe; this is an environment limitation, not a project finding |

Live QRZ/APRS integration calls were not made because the review did not use production credentials. Provider behavior should be verified with sanitized fixtures and stubs before live smoke tests in a controlled environment.

## Recommended first implementation slice

The safest first pull request is deliberately narrow:

1. Add tests reproducing the APRS projection bug, contact error/null cases, and session-field response leakage.
2. Fix those defects and replace raw QRZ response serialization with a minimal public callsign DTO.
3. Remove raw XML logs and add a log-redaction test.
4. Upgrade the vulnerable OpenAPI dependency path.
5. Add a pull-request workflow that builds, tests, and audits dependencies.

That slice removes known correctness/security risks and creates the safety net needed for the session-provider and application-layer refactors that follow.
