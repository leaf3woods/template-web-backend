# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run Commands

```bash
# Build the solution
dotnet build Org.Product.sln

# Run the WebApi project (Development profile)
dotnet run --project src/Org.Product.WebApi/Org.Product.WebApi.csproj

# Run with specific profile
dotnet run --project src/Org.Product.WebApi/Org.Product.WebApi.csproj --launch-profile https

# Run tests (if any exist)
dotnet test

# Build for production
dotnet publish src/Org.Product.WebApi/Org.Product.WebApi.csproj -c Release
```

## Architecture Overview

This is a .NET 8 Web API project following Domain-Driven Design (DDD) with clear layer separation:

```
Org.Product.WebApi        → Controllers, middleware, Program.cs
Org.Product.Application   → Application services, DTOs, authentication, AutoMapper profiles
Org.Product.Domain        → Entities, domain services, value objects, interfaces
Org.Product.Infrastructure → EF Core DbContext, repository implementations, domain service impls
Org.Product.Domain.Shared          → Shared utilities, options, exceptions, extensions
```

### Key Architectural Patterns

**Dependency Injection**: Uses Autofac (not Microsoft DI). Modules are registered via `AppServiceModule`, `AuthModule`, `SingletonModule` in `src/Org.Product.WebApi/Utilities/InjectionModules/`.

**Authentication**: JWT Bearer tokens with ECDSA keys. Keys are loaded from `keys/` folder at startup via `CryptoUtil.Initialize()`. The `JwtTokenUtil` in Application layer handles token generation.

**Authorization**: Permission-based using `[PermissionDefinition]` attribute on methods. Policies are dynamically built in `Program.cs` and `AuthPolicyExtension`.

**Database**: PostgreSQL via EF Core with Npgsql. Uses snake_case naming convention (`UseSnakeCaseNamingConvention()`). Soft delete is implemented via `ISoftDelete` interface with global query filter in `SoftDeleteQueryExtension`.

**Domain Entities**: Base classes in `Org.Product.Domain/Entities/Base/`:
- `AggregateRoot` → base marker interface
- `UniversalEntity` → entity with `Guid Id`
- `IncrementEntity` → entity with `int Id`
- `IAudited`, `ICreationAudited`, `ILastModificationAudited` → audit fields

**Entity Relationships**:
- `User` has many `Role` (via `UserRole` join table)
- `Role` has many `Permission` (via `RolePermission` join table)
- `Permission` has self-referencing `Parent` relationship

**Event Handling**: MediatR is used for domain events (e.g., `UserLoginEvent` handled by `UserLoginEventHandler`).

**DTOs**: Located in `Org.Product.Application/Dtos/`. Base classes: `CreateDto`, `UpdateDto`, `ReadDto`, `QueryDto`.

## Configuration

Key settings in `appsettings.json`:
- `ConnectionStrings:Postgres` → Database connection
- `ConnectionStrings:Redis` → Cache connection
- `Jwt:KeyFolder` → ECDSA key folder path
- `Serilog` → Logging configuration

## Important Files

- [Program.cs](src/Org.Product.WebApi/Program.cs) → Application startup, DI configuration, middleware pipeline
- [ApiDbContext.cs](src/Org.Product.Infrastructure/DbContexts/ApiDbContext.cs) → EF Core context, entity configurations, soft delete setup
- [PermissionAuthorizationRequirement.cs](src/Org.Product.Application/Auth/Requirements/PermissionAuthorizationRequirement.cs) → Custom authorization requirement
- [ResponseWrapper.cs](src/Org.Product.WebApi/Utilities/ResponseWrapper.cs) → API response wrapper
- [ExceptionExtension.cs](src/Org.Product.WebApi/Utilities/ExceptionExtension.cs) → Global exception handling
