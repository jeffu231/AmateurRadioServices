# Repository Guidelines

## Project Structure & Module Organization

This repository contains one ASP.NET Core Web API solution. `CoreServices.sln` is the solution entry point, and `CoreServices/CoreServices.csproj` targets .NET 10 using the SDK pinned in `global.json`. Application startup and dependency registration live in `CoreServices/Program.cs`. HTTP endpoints are in `CoreServices/Controllers`, external integrations are in `CoreServices/Services`, and DTO/domain models are in `CoreServices/Model` with subfolders for APRS and QRZ-specific XML data. Runtime settings are in `CoreServices/appsettings*.json`; Docker configuration is in `CoreServices/Dockerfile`, `docker-compose.yml`, and `docker-compose.dev.yml`.

## Build, Test, and Development Commands

- `dotnet restore CoreServices.sln`: restores NuGet packages.
- `dotnet build CoreServices.sln`: compiles the API and validates project references.
- `dotnet run --project CoreServices/CoreServices.csproj`: runs the service locally.
- `dotnet publish CoreServices/CoreServices.csproj -c Release`: creates a release build.
- `docker compose up --build`: builds and runs the container stack with Traefik labels from the compose file.

Create a local `.env` for Docker Compose with `QrzUsername`, `QrzPassword`, `AprsApiKey`, and `HOSTNAME`. Do not commit real credentials.

## Coding Style & Naming Conventions

Use C# with nullable reference types and implicit usings enabled. Follow the existing namespace and folder conventions: controllers end with `Controller`, services end with `Service`, and model names describe API payloads or radio concepts such as `AprsEntry` and `ContactInfo`. Prefer constructor or DI-based dependencies over static service access. Keep JSON-facing behavior camelCase, matching `Program.cs` serializer configuration. Use four-space indentation for C# and keep comments limited to non-obvious behavior.

## Testing Guidelines

No test project is currently committed. Add tests in a sibling project such as `CoreServices.Tests` and include it in `CoreServices.sln`. Prefer xUnit or NUnit for unit tests and `Microsoft.AspNetCore.Mvc.Testing` for endpoint tests. Name test files after the unit under test, for example `MaidenheadControllerTests.cs`, and run all tests with `dotnet test CoreServices.sln`.

## Commit & Pull Request Guidelines

Commit history and Husky hooks enforce Conventional Commit-style subjects. Use types such as `feat`, `fix`, `docs`, `test`, `refactor`, `ci`, `build`, or `chore`, optionally scoped: `fix(qrz): support suffix callsigns`. Keep the first line under 90 characters and do not end it with a period. Pull requests should describe the API behavior changed, list validation performed, link related issues, and include sample requests/responses or screenshots when Swagger or endpoint output changes.
