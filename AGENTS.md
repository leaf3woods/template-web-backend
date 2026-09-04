# Repository Guidelines

## Project Structure & Module Organization

This repository is a .NET 8 Web API solution organized around DDD layers.

- `Org.Product.sln` is the main solution file.
- `src/Org.Product.WebApi/` contains controllers, middleware, and `Program.cs`.
- `src/Org.Product.Application/` contains services, DTOs, authentication, and mapping.
- `src/Org.Product.Domain/` contains entities, domain services, events, and interfaces.
- `src/Org.Product.Infrastructure/` contains EF Core persistence and repositories.
- `src/Org.Product.Domain.Shared/` contains shared exceptions, utilities, and extensions.
- `deployment/` and `doc/` hold deployment and project documentation assets.

## Build, Test, and Development Commands

Run commands from the repository root.

```bash
dotnet build Org.Product.sln
dotnet run --project src/Org.Product.WebApi/Org.Product.WebApi.csproj
dotnet run --project src/Org.Product.WebApi/Org.Product.WebApi.csproj --launch-profile https
dotnet publish src/Org.Product.WebApi/Org.Product.WebApi.csproj -c Release
dotnet test
```

`dotnet build` validates the full solution. `dotnet run` starts the Web API locally. `dotnet publish` creates a Release build for deployment. `dotnet test` is the standard test entry point; add future test projects to the solution so this command remains complete.

## Coding Style & Naming Conventions

Use nullable reference types and implicit usings. Keep one public class per file and match file names to class names. Use `PascalCase` for classes, methods, and properties; `_camelCase` for private fields; `I` prefixes for interfaces; and `*Dto` suffixes for DTOs. Database names should remain `snake_case` through EF Core conventions.

Place base entities under `Domain/Entities/Base/`. Use `Guid` IDs for universal entities and `int` IDs for auto-increment entities. Prefer custom exceptions from `Org.Product.Domain.Shared.Exceptions` for API errors.

## Testing Guidelines

No test project is currently listed in `Org.Product.sln`. When adding tests, create or restore `src/Org.Product.Tests/`, add it to the solution, and keep test names behavior-focused, for example `CreateUser_WhenNameExists_ThrowsBadRequestException`. Run targeted tests with:

```bash
dotnet test --filter "FullyQualifiedName~TestMethodName"
```

## Commit & Pull Request Guidelines

Git history currently only establishes `init`, so keep commits concise, imperative, and scoped, for example `Add user permission query`. Pull requests should include a short summary, related issue or task, database/configuration changes, and test results. Include API examples or screenshots when response shapes or Swagger-visible behavior changes.

## Security & Configuration Tips

Do not commit secrets. Configure PostgreSQL, Redis, JWT key paths, and Serilog through `appsettings.json`, environment-specific overrides, or environment variables. Verify authentication and permission-policy changes against the dynamic policy format: `{ManagedResource}.{ManagedAction}.{Suffix}`.
