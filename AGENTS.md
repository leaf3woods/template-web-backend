# AGENTS.md - Guidance for AI Agents

This file provides guidance for AI coding agents working in this repository.

## Build, Run & Test Commands

```bash
# Build the entire solution
dotnet build TemplateWeb.sln

# Run the WebApi project (Development profile)
dotnet run --project src/Template.Web.WebApi/Template.Web.WebApi.csproj

# Run with specific launch profile
dotnet run --project src/Template.Web.WebApi/Template.Web.WebApi.csproj --launch-profile https

# Build for production
dotnet publish src/Template.Web.WebApi/Template.Web.WebApi.csproj -c Release

# Run all tests
dotnet test

# Run a single test (filter by name)
dotnet test --filter "FullyQualifiedName~TestMethodName"

# Run tests in a specific project
dotnet test src/Template.Web.Tests/Template.Web.Tests.csproj
```

## Architecture Overview

This is a .NET 8 Web API using Domain-Driven Design (DDD):

| Project | Responsibility |
|---------|----------------|
| Template.Web.WebApi | Controllers, middleware, Program.cs |
| Template.Web.Application | Services, DTOs, authentication, AutoMapper |
| Template.Web.Domain | Entities, domain services, interfaces |
| Template.Web.Infrastructure | EF Core DbContext, repositories |
| Template.Web.Core | Shared utilities, exceptions, extensions |

## Code Style Guidelines

### Naming Conventions
- **Classes/Methods/Properties**: `PascalCase`
- **Private fields**: `_camelCase` (e.g., `_userService`)
- **Interfaces**: `I` prefix (e.g., `IUserService`)
- **DTOs**: `PostfixDto` (e.g., `UserReadDto`, `CreateDto`)
- **Database columns**: `snake_case` (enforced via EF Core `UseSnakeCaseNamingConvention()`)

### File Organization
- One class per file; filename matches class name
- Group related code with `#region` blocks (e.g., `#region navigation`, `#region audit`)
- Follow namespace-to-folder correspondence

### Imports
- Use implicit usings (enabled in all projects)
- Explicit imports for project references only
- Order: System namespaces → Project namespaces (alphabetical)

### Types
- Enable nullable reference types: `<Nullable>enable</Nullable>`
- Use `string!` for properties that are initialized but analyzer can't verify
- Prefer `Guid` for entity IDs, `int` for auto-increment entities
- Base entity classes in `Domain/Entities/Base/`:
  - `UniversalEntity` → `Guid Id`
  - `IncrementEntity` → `int Id`
  - `AggregateRoot` → marker interface

### Error Handling
- Throw custom exceptions from `Template.Web.Core.Exceptions`:
  - `NotFoundException` - resource not found (404)
  - `BadRequestException` - invalid input (400)
  - `NotAcceptableException` - business rule violation (406)
  - `ForbiddenException` - authorization failure (403)
- All exceptions inherit from `CustomException`
- Global exception handling via `ExceptionExtension.cs`

### Documentation
- XML documentation on all public members (`<summary>`, `<param>`)
- Use Chinese comments for domain-specific terms (用户名, 角色)
- Keep comments concise; explain "why" not "what"

## Key Patterns

### Dependency Injection
- Uses **Autofac** (not Microsoft DI)
- Register modules in `Utilities/InjectionModules/`:
  - `AppServiceModule` - application services
  - `AuthModule` - authentication/authorization
  - `SingletonModule` - singleton services

### Authorization
- Permission-based via `[PermissionDefinition]` attribute
- Policies dynamically built in `Program.cs`
- Format: `"{ManagedResource}.{ManagedAction}.{Suffix}"` (e.g., `User.Get.Id`)

### Entity Relationships
- User ↔ Role: many-to-many via `UserRole` join table
- Role ↔ Permission: many-to-many via `RolePermission` join table
- Permission self-references via `Parent`

### Domain Events
- Use **MediatR** for domain events
- Handlers implement `IRequestHandler<TEvent, TResponse>`

### DTOs
- Base classes in `Application/Dtos/Base/`:
  - `CreateDto` - for POST requests
  - `UpdateDto` - for PUT/PATCH requests
  - `ReadDto` - for responses
  - `QueryDto` / `PaginatedQueryDto` - for queries

### Database
- PostgreSQL via EF Core with Npgsql
- Soft delete via `ISoftDelete` interface
- Global query filter in `SoftDeleteQueryExtension`

## Response Wrapping
- All controller responses wrapped via `ResponseWrapper<T>.Wrap()`
- Returns standardized API response format

## Configuration
Key settings in `appsettings.json`:
- `ConnectionStrings:Postgres` - database
- `ConnectionStrings:Redis` - cache
- `Jwt:KeyFolder` - ECDSA key path for JWT signing
- `Serilog` - logging configuration
