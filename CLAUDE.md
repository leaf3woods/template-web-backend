# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run Commands

```bash
# Build the solution
dotnet build TemplateWeb.sln

# Run the WebApi project (Development profile)
dotnet run --project src/Template.Web.WebApi/Template.Web.WebApi.csproj

# Run with specific profile
dotnet run --project src/Template.Web.WebApi/Template.Web.WebApi.csproj --launch-profile https

# Run tests (if any exist)
dotnet test

# Build for production
dotnet publish src/Template.Web.WebApi/Template.Web.WebApi.csproj -c Release
```

## Architecture Overview

This is a .NET 8 Web API project following Domain-Driven Design (DDD) with clear layer separation:

```
Template.Web.WebApi        → Controllers, middleware, Program.cs
Template.Web.Application   → Application services, DTOs, authentication, AutoMapper profiles
Template.Web.Domain        → Entities, domain services, value objects, interfaces
Template.Web.Infrastructure → EF Core DbContext, repository implementations, domain service impls
Template.Web.Core          → Shared utilities, options, exceptions, extensions
```

### Key Architectural Patterns

**Dependency Injection**: Uses Autofac (not Microsoft DI). Modules are registered via `AppServiceModule`, `AuthModule`, `SingletonModule` in `src/Template.Web.WebApi/Utilities/InjectionModules/`.

**Authentication**: JWT Bearer tokens with ECDSA keys. Keys are loaded from `keys/` folder at startup via `CryptoUtil.Initialize()`. The `JwtTokenUtil` in Application layer handles token generation.

**Authorization**: Permission-based using `[PermissionDefinition]` attribute on methods. Policies are dynamically built in `Program.cs` and `AuthPolicyExtension`.

**Database**: PostgreSQL via EF Core with Npgsql. Uses snake_case naming convention (`UseSnakeCaseNamingConvention()`). Soft delete is implemented via `ISoftDelete` interface with global query filter in `SoftDeleteQueryExtension`.

**Domain Entities**: Base classes in `Template.Web.Domain/Entities/Base/`:
- `AggregateRoot` → base marker interface
- `UniversalEntity` → entity with `Guid Id`
- `IncrementEntity` → entity with `int Id`
- `IAudited`, `ICreationAudited`, `ILastModificationAudited` → audit fields

**Entity Relationships**:
- `User` has many `Role` (via `UserRole` join table)
- `Role` has many `Permission` (via `RolePermission` join table)
- `Permission` has self-referencing `Parent` relationship

**Event Handling**: MediatR is used for domain events (e.g., `UserLoginEvent` handled by `UserLoginEventHandler`).

**DTOs**: Located in `Template.Web.Application/Dtos/`. Base classes: `CreateDto`, `UpdateDto`, `ReadDto`, `QueryDto`.

## Configuration

Key settings in `appsettings.json`:
- `ConnectionStrings:Postgres` → Database connection
- `ConnectionStrings:Redis` → Cache connection
- `Jwt:KeyFolder` → ECDSA key folder path
- `Serilog` → Logging configuration

## Important Files

- [Program.cs](src/Template.Web.WebApi/Program.cs) → Application startup, DI configuration, middleware pipeline
- [ApiDbContext.cs](src/Template.Web.Infrastructure/DbContexts/ApiDbContext.cs) → EF Core context, entity configurations, soft delete setup
- [PermissionAuthorizationRequirement.cs](src/Template.Web.Application/Auth/Requirements/PermissionAuthorizationRequirement.cs) → Custom authorization requirement
- [ResponseWrapper.cs](src/Template.Web.WebApi/Utilities/ResponseWrapper.cs) → API response wrapper
- [ExceptionExtension.cs](src/Template.Web.WebApi/Utilities/ExceptionExtension.cs) → Global exception handling
