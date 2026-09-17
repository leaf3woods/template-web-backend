# Repository Guidelines

## Project Structure & Module Organization

`Org.Product.sln` contains .NET 10 projects under `src/`:

- `Org.Product.WebApi`: controllers, authentication, dependency composition, and localized `Resources/*.resx`.
- `Org.Product.Application`: use cases, DTOs, mappings, and capability abstractions.
- `Org.Product.Domain` / `Org.Product.Domain.Shared`: entities, repository contracts, and shared types.
- `Org.Product.Infrastructure`: EF Core repositories/migrations, Redis, JWT, and protocol adapters.
- `Org.Product.DbMigrator`: standalone database migration process.
- `Org.Product.Tests`: automated tests and `Support/` doubles.

Deployment examples live in `deployment/`; template metadata lives in `.template.config/`. Preserve the `Org.Product` naming token when extending the template. Use solution-listed projects; legacy `*Backup*` project files are excluded from template generation.

## Architecture Boundaries

Application depends on Domain and Domain.Shared; Infrastructure implements Application/Domain contracts. Keep concrete adapters out of Application and Domain. WebApi composes dependencies. Application currently permits `IQueryable` and EF asynchronous query operations. Each mutation use case owns its unit-of-work commit. See `docs/architecture.md` before changing boundaries.

For dependency injection, use a framework-provided host `AddXxx` API before writing an equivalent Autofac registration. Use parameterless Autofac modules discovered by WebApi for application-specific adapters. Scan implementations from a stable semantic interface root and set lifetime at that root; keep generic contracts, third-party client factories, and framework APIs requiring a concrete type as explicit module registrations. Do not add routine business-service registrations to `Program.cs`.

## Build, Test, and Development Commands

Run from the repository root with the .NET 10 SDK:

```powershell
dotnet restore Org.Product.sln
dotnet build Org.Product.sln --no-restore
dotnet test Org.Product.sln --no-build
dotnet run --project src/Org.Product.WebApi/Org.Product.WebApi.csproj --launch-profile https
```

These restore dependencies, compile, execute tests, and launch the development API with Swagger. Configure PostgreSQL and Redis before starting the API.

`dotnet tool restore` restores EF tooling. Follow the [database migration guide](docs/README.en.md#database-migrations) for SQL generation and migrations; WebApi does not migrate schemas at startup.

## Coding Style & Naming Conventions

Use four-space indentation, nullable reference types, explicit accessibility modifiers, `var` for local variables, PascalCase types/methods, `I`-prefixed interfaces, `_camelCase` private fields, and `Async` suffixes. Prefer explicit constructors and match surrounding namespace style. The root `.editorconfig` enforces LF endings, final newlines, braces, readonly private fields, removal of unused `using` directives, and file-scoped namespaces. Top-level statements and primary constructors remain disabled pending an explicit team-wide style decision. EF Core migrations under `src/Org.Product.Infrastructure/Repositories/Migrations` are versioned generated artifacts and are excluded from formatting. For scoped hand-written source, use `dotnet format Org.Product.sln whitespace --include <changed-files> --exclude src/Org.Product.Infrastructure/Repositories/Migrations` and `dotnet format Org.Product.sln style --diagnostics IDE0005 IDE0007 IDE0011 IDE0040 IDE0044 IDE0161 --severity hidden --include <changed-files> --exclude src/Org.Product.Infrastructure/Repositories/Migrations`.

## Testing Guidelines

Use xUnit, Moq, and ASP.NET Core TestHost. Name classes `*Tests` and methods `Scenario_ExpectedBehavior`. Add focused regression tests for changed behavior, including authorization failures and persistence boundaries. Reuse `Support/` doubles; existing tests avoid live PostgreSQL/Redis dependencies. No coverage percentage is configured. Run a subset with `dotnet test Org.Product.sln --filter FullyQualifiedName~AuthorizationTests`.

## Commit & Pull Request Guidelines

Follow history's Conventional Commits, such as `feat(database): add standalone migration process`. Keep changes focused. PRs should describe behavior, link applicable issues, report validation, and explain configuration or migration impacts. Include API request/response examples when contracts change. Keep credentials and generated signing keys out of commits; use environment variables or WebApi user secrets.
