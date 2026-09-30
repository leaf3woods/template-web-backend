# Architecture

This document describes the implementation in `Org.Product.sln`: a .NET 10 ASP.NET Core backend template with account, role, and menu/permission examples. It separates project dependencies, runtime collaboration, and current implementation limits. Deployment examples and extension points do not imply that an integration is operational.

Use [Repository Guidelines](../AGENTS.md) for contribution commands and conventions, and the [database migration guide](README.en.md#database-migrations) for database operations.

## 1. Projects and dependency direction

All active projects live under `src/`. The table lists direct project references; package dependencies are separate.

| Project | Responsibility | Direct project references |
| --- | --- | --- |
| `Org.Product.WebApi` | HTTP controllers, authentication/authorization, responses, and composition root | Application, Infrastructure, Domain, Domain.Shared |
| `Org.Product.Application` | Use cases, DTOs, mapping, and required capability contracts | Domain, Domain.Shared |
| `Org.Product.Domain` | Entities, value objects, repository/unit-of-work contracts, and event definitions | Domain.Shared |
| `Org.Product.Domain.Shared` | Shared exceptions, attributes, enums, pagination, and serialization helpers | None |
| `Org.Product.Infrastructure` | PostgreSQL persistence, Redis stores, security implementations, and wire protocols | Application, Domain, Domain.Shared |
| `Org.Product.DbMigrator` | One-shot database migration host and EF design-time factory | Infrastructure |
| `Org.Product.Tests` | Unit, HTTP authorization, and architecture regression tests | WebApi |

```mermaid
flowchart TD
    Tests --> WebApi
    WebApi --> Application
    WebApi --> Infrastructure
    WebApi --> Domain
    WebApi --> Shared[Domain.Shared]
    DbMigrator --> Infrastructure
    Infrastructure --> Application
    Infrastructure --> Domain
    Infrastructure --> Shared
    Application --> Domain
    Application --> Shared
    Domain --> Shared
```

Arrows mean compile-time project references. Application calls interfaces whose implementations are selected by WebApi; those runtime calls do not require an Application-to-Infrastructure reference.

The code follows layered responsibilities with inward-facing contracts, with deliberate framework coupling: Application references EF Core for LINQ execution, AutoMapper, MediatR, and options; Domain references MediatR for `INotification`. It is not a framework-free domain/application model. `Domain.Shared` is a shared library, not a dependency-injection host.

## 2. Hosts and dependency composition

[WebApi/Program.cs](../src/Org.Product.WebApi/Program.cs) owns application startup. It configures options, Serilog, controllers, JWT authentication, authorization policies, localization, Swagger, persistence, security adapters, AutoMapper, and MediatR.

Registration remains divided by responsibility:

| Registration | Responsibilities and lifetimes |
| --- | --- |
| `ApplicationModule` | Discovers Application implementations of `IBaseService`, registers their interfaces per lifetime scope, and registers the pagination converter |
| `AuthorizationModule` | Discovers pure Domain services through `IDomainService`, authorization handlers through `IAuthorizationHandler`, and JWT event types through their framework base class |
| `AddAllOptions` | Binds and validates application captcha policy, JWT validation/issuance, captcha rendering, and OpenAPI configuration through the host `IServiceCollection` |
| `AddDbContextPool<ApiDbContext>` | Native EF Core pooled request-scoped context registration in WebApi; Autofac imports it from the host service collection |
| `PersistenceModule` | Generic `IRepository<>` and `ICacheQuery<,,>` mappings plus scanned scoped `IUnitOfWork` and `ISqlExecutor` implementations |
| `SecurityInfrastructureModule` | Scanned `ISecurityStore` implementations per scope; scanned stateless `ISecurityService` implementations as singletons; explicit Redis connection and signing-key infrastructure |
| `DbMigrator` `Program` | Its own pooled `ApiDbContext` and `DatabaseMigrator`; it does not register HTTP, Redis, JWT, or captcha components |

Autofac is the WebApi service provider and WebApi is the composition root for the HTTP process. It automatically discovers parameterless `Module` types in the WebApi and Application assemblies. Use host `AddXxx` APIs for framework-owned integrations such as EF Core context pooling, options, authentication, controllers, and Swagger. Autofac modules scan implementation assemblies from a stable interface root whenever a semantic lifetime category exists. Registration callbacks remain for third-party client factories and framework APIs that require a concrete type. The separate DbMigrator host uses the standard host/service collection and creates only its own persistence registrations plus `DatabaseMigrator`. Its assembly dependency on Infrastructure does not start Redis, token issuance, or image rendering.

Keep host-specific configuration and transport behavior in the host. Put new external implementations in Infrastructure and their use-case-facing contracts in Application, unless the contract represents a domain responsibility such as repository access.

## 3. HTTP request and application flow

```mermaid
flowchart LR
    Client --> Auth[JWT validation and Redis session check]
    Auth --> Policy[Endpoint permission policy]
    Policy --> Controller
    Controller --> Service[Application service]
    Service --> Repository[IRepository / IUnitOfWork]
    Repository --> EF[ApiDbContext / PostgreSQL]
    Service --> Store[Security capability interfaces]
    Store --> Adapter[Redis / JWT / captcha adapters]
    Service --> DTO[DTO mapping]
    DTO --> Response[Controller response]
    Response --> Client
```

This is the protected-request collaboration flow, not a literal middleware registration diagram. Explicitly anonymous endpoints bypass authorization requirements.

- `HomeController` exposes captcha, login, registration, and authenticated logout.
- `UserController` and `RoleController` delegate account/role operations to Application services.
- `MenuController` exposes an anonymous menu tree and protected menu mutations. Menus are backed by `Permission` entities.
- `CrudAppService` provides reusable single-resource CRUD. Resource services supply their queries and behavior such as user initialization, role assignment, session revocation, and menu deletion constraints.

Controllers commonly return `ResponseWrapper<T>` with `Info`, `Data`, and `Status`. The wrapper's `Status` field is body data; HTTP status still belongs to the HTTP layer. Logout, authentication failures, model validation, and exceptions do not all use this envelope.

The exception handler writes `ExceptionReadDto`. Its current status mapping is `NotFoundException` to 404, `NotAcceptableException` to 406, and other exceptions to 500, including `ForbiddenException`. Selected custom messages use localization resources. Development responses can include stack/inner-exception details; other environments still return the top-level message. Localization resources exist, but startup does not configure request-culture middleware. See [ExceptionLocalizerExtension](../src/Org.Product.WebApi/Utilities/ExceptionLocalizerExtension.cs).

## 4. Domain and persisted model

| Model | Meaning and relationships |
| --- | --- |
| `User` | Credentials, profile, state, and roles; owns `UserSetting` and `UserDetail` through EF owned mappings |
| `Role` | Role metadata; many-to-many users through `UserRole`, and permissions through `RolePermission` |
| `Permission` | Shared catalogue/menu/button/API model, identified by `PermissionType`; `ParentId` represents its tree |
| `AggregateRoot<TKey>` | Identity and repository eligibility; currently no domain-event collection or automatic dispatch |
| `ISoftDelete`, `IAudited`, `IEnableable`, `IHasSortOrder` | Entity capabilities; implementing an interface does not itself enforce a business rule or populate audit fields |

Domain entities currently expose mutable state, while much of the orchestration and validation lives in Application services. For example, `MenuService` rejects deleting the root or a node with children, and `UserService.ChangeRoleAsync` requires every requested role to exist before saving.

Do not conflate persisted `Permission` rows, `PermissionDefinitionAttribute` metadata on services, endpoint policy names, and projected permission codes. They are separate representations. Startup builds authorization policies from controller `[Authorize(Policy = ...)]` attributes; it does not automatically publish service metadata into database rows.

## 5. Persistence, queries, and commit ownership

[ApiDbContext](../src/Org.Product.Infrastructure/Repositories/ApiDbContext.cs) uses PostgreSQL through Npgsql and snake_case naming. It defines relationships, unique indexes, owned values, seed data, and soft-delete query filters. Concrete persistence stays in Infrastructure:

- `IRepository<TEntity>` belongs to Domain. `Repository<TEntity>` exposes tracking queries by default, optional no-tracking queries, lookup, and change-tracker mutations. Add/update/remove do not save automatically.
- `IUnitOfWork` owns `SaveChangesAsync` and explicit transaction creation. Repository instances, `UnitOfWork`, and `SqlExecutor` share the scoped `ApiDbContext`.
- `ISqlExecutor` belongs to Application. Its `SqlCommand`/`SqlParameter` types carry SQL and parameter values without exposing `NpgsqlParameter`; Infrastructure performs the conversion. SQL executes directly, so it must not be treated as a queued repository change or assumed to apply EF entity filters.

Application intentionally composes `IQueryable` queries and uses EF methods such as `Include`, `ToListAsync`, and pagination helpers. This keeps queries convenient but retains EF/provider behavior at the Application boundary. Avoid returning deferred queries to controllers; add use-case-specific query contracts when needed.

Each public mutation use case owns its database save. Compose repository operations within that use case instead of chaining independently committing services. For multiple saves/raw SQL requiring one transaction, use `BeginTransactionAsync`, save as needed, then explicitly commit; beginning a second transaction in the same scope is rejected. Transaction commit does not replace `SaveChangesAsync`.

`ApiDbContext.SaveChangesAsync` converts deleted `ISoftDelete` entries into `SoftDeleted = true` and a UTC deletion timestamp. Model query filters hide those rows from normal queries. This is not a blanket guarantee for raw SQL or tracked lookup behavior, and it does not implement automatic audit stamping or application-level cascading soft deletion.

Database saves and Redis writes are separate operations. User changes save first and revoke sessions afterward. A database failure therefore prevents revocation, but a Redis failure after a successful save can leave committed data with an unreconciled session. There is no cross-store transaction, outbox, or retry worker closing that gap.

## 6. DTOs and mapping

Application owns request/query/read DTOs and AutoMapper profiles. WebApi discovers profiles from the Application assembly.

`DtoConventionProfile` scans concrete DTOs derived from `ReadDto<TEntity>`, `ReadDto<TEntity, TKey>`, `CreateDto<TEntity>`, and `UpdateDto<TEntity>`. Read mappings run entity-to-DTO; create/update mappings run DTO-to-entity. Types implementing `IManualDtoMapping` are excluded and require an explicit profile. The pagination converter maps `PaginatedList<T>` results.

Mapping should transform data without database access, role assignment, credential initialization, or commits. `UserRegisterDto` has a manual mapping that ignores credentials and roles; both `UserService.CreateAsync` and `RegisterAsync` use the explicit registration path to set credentials, assign the member role, and save once. Custom field names and relationships require explicit mappings rather than assumptions about convention matching.

## 7. Authentication, sessions, and permissions

### Login and token lifecycle

1. `UserService.LoginAsync` optionally verifies a captcha according to Application options.
2. It loads the user and roles, then calls `IPasswordCredentialService.Verify`.
3. `IAccessTokenIssuer` issues an ES256 token containing the user ID, comma-separated role IDs, a fresh `jti`, and configured expiry.
4. `IUserSessionStore.SaveAsync` stores the complete token under the user's Redis key with the token lifetime. A later login replaces the previous token.
5. Protected requests validate the signature, issuer, audience, and lifetime, then `SessionJwtBearerEvents` requires an exact match with the Redis session. This also applies to super-role users.

Logout supplies the authenticated token to `RevokeAsync`. The Redis adapter uses an atomic Lua compare-and-delete so an older in-flight logout cannot remove a newer session. User update/delete, role reassignment, and password change/reset revoke the current session after saving.

[PasswordCredentialService](../src/Org.Product.Infrastructure/Adapters/Security/PasswordCredentialService.cs) currently expects Base64-encoded password bytes and stores a salted SHA-256 result, with fixed-time comparison during verification. This describes the existing credential format; changing it requires an explicit compatibility/migration design. `IsEnabled` alone does not constitute an account-disable policy: the current login path does not check `User.IsEnabled`.

### Endpoint permission evaluation

`AuthPolicyExtensions` installs an authenticated fallback policy and discovers named policies from controller/action attributes. `[AllowAnonymous]` explicitly opens an endpoint. Plain `[Authorize]` requires authentication; a named policy also runs `CustomRequireHandler`.

`ClaimsPrincipalExtension` parses the user ID and complete role ID list for `HttpCurrentUser`, `CustomRequireHandler`, and JWT session validation. An empty GUID or any malformed role ID invalidates the corresponding parsed claim; `ICurrentUser.RoleIds` exposes the full list. The handler validates these claims and reads `RolePermissionsCacheItem` for every distinct claimed role through `ICacheQuery<Role, Guid, RolePermissionsCacheItem>.FindManyWithDetailAsync<RolePermission, Permission>`. It fetches cached roles in one Redis batch and projects cache misses in one database query. A missing role rejects authorization, including when another claimed role is the super role. After all roles are found, the super-role ID can bypass permission matching. Other roles grant access through an exact permission or dot-separated ancestor, using ordinal matching: `menu` grants the current `menu.delete.Id` policy, while `menu.get` does not. Policy strings must match the endpoint's spelling and casing. The current menu create/update DTOs and seed data do not populate `Permission.PermissionCode`, so non-super roles require that field to be provisioned separately before these database-backed checks can grant access.

### Redis semantics and consistency limits

Cache contracts live in `Application/Abstractions/CacheStore`; security-store contracts live under the other Application abstraction folders. Redis implementations live in Infrastructure adapters.

Application defines the strongly typed `ICacheQuery<TEntity, TKey, TCacheItem>` contract for ordinary cache-backed entity and filtered-list reads. Its entity type derives from `AggregateRoot<TKey>` so a projected lookup can use the strongly typed `Id` property. `RedisCacheQuery` implements cache-aside reads in Infrastructure and is registered as an open generic by WebApi. `FindWithDetailAsync` projects the entity by ID through `ProjectTo<TCacheItem>`; its root-only, one-relation, and two-relation overloads use the declared entity generations, ID, and projection type in a separate cache key without a predicate fingerprint. `FindManyWithDetailAsync` has the same dependency overloads, reads cache keys with MGET, and queries missing IDs together. Detail lookups omit missing IDs from batch results and return `null` for a missing single ID; ordinary `FindAsync` raises `NotFoundException`. `GetFilteredListAsync` is a terminal, non-paged complete-collection operation. It caches only predicates composed from constants, parameters, member access, and supported comparison, boolean, and conversion operators; method-call nodes, static member access, unlisted operators, and unsupported complex captured values bypass Redis rather than risk an incorrect cache hit. Supported predicates build a SHA-256 fingerprint from node/result types, member/operator identities, and length-prefixed culture-invariant scalar values. Parameter names do not affect the key, while predicate structure and captured values do. Filtered-list keys include the projection type and carry their own key-format version so changing their format does not invalidate unrelated session, captcha, or permission entries. The original predicate is used for the database query, so callers must not mutate captured values concurrently while `GetFilteredListAsync` is running.

On a cache miss or an unsupported predicate, `RedisCacheQuery` projects at most `Cache:MaxCollectionItems + 1` matching rows. It throws `CacheQueryResultTooLargeException` when the configured bound is exceeded, without caching a partial collection. Use a paginated query service instead when the complete result is not naturally bounded.

`CacheItem<TEntity, TKey>` reuses the existing DTO convention to register entity-to-cache-item mappings automatically. `UserCacheItem` and `RoleCacheItem` expose only query-safe fields, while `PermissionCacheItem` maps directly from `Permission<Guid>` and `RolePermissionsCacheItem` has an explicit relationship projection; domain aggregates do not implement cache contracts, and the user projection excludes credentials and navigation properties. The separate `ICacheInvalidator` contract deletes exact entity-cache keys or explicit store-cache keys without giving query consumers mutation capabilities. Ordinary `FindAsync` keys include the cache-item type and root entity generation, so multiple projections of one ID cannot share a value and any root-entity change invalidates them. Filtered lists and projected entity reads also include the root entity's Redis generation in the key by default. The generic relation overloads declare up to two additional aggregate-root dependencies for a query whose predicate or projection reads their data. The query reads the root and declared relation generations and includes each entity name and generation in a fixed order in the key. `UnitOfWork` collects added, modified, and deleted entity types and primary keys; after a successful non-transactional save or explicit transaction commit, it deletes exact legacy entity-cache entries and increments each affected type's generation. Old generation-based keys are not deleted, but expire after `Cache:ExpireSeconds`. Generation counters do not expire. Dependencies are declared by the chosen overload; the adapter does not infer them from EF navigation properties or AutoMapper projections. They do not apply to the unused role-permission snapshot store. Do not evict or delete a generation counter while older generation-based keys can remain: resetting it to zero could reuse an old key. Database commits and Redis invalidation are not atomic, and no retry or compensation worker is implemented.

| Capability | Redis key | Current behavior |
| --- | --- | --- |
| `IUserSessionStore` | `sys:token:{userId}` | One current token per user; login TTL follows JWT lifetime; conditional or unconditional revocation |
| `ICaptchaChallengeStore` | `sys:captcha:{captchaId}` | Stores an answer with configured expiry; verification reads and compares without consuming the challenge |
| `IRolePermissionStore` | `sys:permissions:{roleId}` | Legacy JSON permission snapshot adapter; no longer used by `CustomRequireHandler` |

No caller populates the legacy snapshots through `IRolePermissionStore.SaveAsync`; they are not part of the active authorization path. Role permission projections use the `Role`, `RolePermission`, and `Permission` generations. `RoleService.ModifyRoleScopeAsync` commits through `UnitOfWork`, which advances generations for tracked changes. A database commit and the subsequent Redis invalidation remain non-atomic.

## 8. Other adapters and event boundaries

`ICaptchaGenerator` is implemented by `SkiaCaptchaGenerator`. Application owns verification/expiry decisions and passes runtime generation settings; Infrastructure merges those settings with configured renderer defaults and owns fonts, dimensions, rendering, and image bytes. Character captcha is the default, callers can select Character or Question, and Han currently throws `NotSupportedException` because its builder is not implemented. The store does not implement attempt limits or one-time consumption.

`Infrastructure/Adapters/Protocols` contains binary frame, CRC, and JSON message examples. These are wire-format adapters, not domain entities or a running device-message ingestion service. The Compose MQTT service does not establish an application MQTT consumer or publisher.

MediatR scans Application handlers, and Domain declares `UserLoginEvent`. The login use case does not publish it; `UserLoginEventHandler.Handle` currently throws `NotImplementedException`. There is no implemented durable event bus, aggregate event dispatch, or outbox. Other explicit placeholders include `CrudAppService.UpdateEnabledAsync`, `MenuService.SetMenuRouteAsync`, and the empty `SettingService`.

## 9. Configuration and runtime assets

WebApi uses standard ASP.NET Core configuration sources, including environment-specific settings, Development user secrets, and environment variables. Options are owned by their consumers rather than collected in Domain.

| Configuration | Consumer and purpose |
| --- | --- |
| `ConnectionStrings:Postgres` | WebApi `AddDbContextPool<ApiDbContext>` and DbMigrator's local persistence registration; both must target the same database |
| `ConnectionStrings:Redis` | Shared Redis connection for sessions, captcha challenges, and permission snapshots |
| `Jwt:Issuer`, `Audience`, `KeyFolder` | WebApi authentication and signing-key loading |
| `Jwt:ExpireMin` | Security module's token issuer and resulting session TTL |
| `Captcha:RequireVerification`, `ExpireSeconds` | Application captcha policy |
| `Captcha:FontFamily`, dimensions, decoration flags | Security module's Infrastructure-backed image renderer |
| `Cache:ExpireSeconds`, `MaxCollectionItems` | Generic Redis cache lifetime and maximum complete filtered-list size |
| `OpenApiInfo` | WebApi Swagger document metadata |
| `Serilog` | WebApi logging configuration |

Required JWT, captcha, and OpenAPI options have startup validation; this does not prove database/Redis connectivity or rendering support. Swagger is enabled in Development. No readiness/health endpoint is registered in the current startup.

### Options and settings naming

The `Options` suffix is reserved for values loaded from an executable host's configuration sources and consumed through the .NET options pattern. A configuration options type declares its configuration section, is bound explicitly by the composition root, and validates required values at startup. For example, `CaptchaRenderingOptions` represents renderer defaults supplied by `appsettings.json`, environment variables, user secrets, or another registered configuration provider.

Use the `Settings` suffix for equivalent runtime or per-call values created by application code and passed directly to a component. Settings do not declare a configuration section, are not registered with `AddOptions<T>`, and are not injected through `IOptions<T>`. Names such as `Request`, `Command`, `Dto`, `Policy`, or `Preferences` remain preferable when they describe a more specific transport or domain role; the convention does not rename every non-configuration object to `Settings`.

When runtime settings selectively override configured defaults, the adapter that owns the behavior merges options and settings exactly once, validates the resolved values, and passes a complete result to lower-level code. Prefer nullable members when omission must be distinguished from valid default values. A documented sentinel such as zero is also acceptable when the value cannot be valid and the resolved result is still validated. Avoid ambiguous sentinels when zero, an empty string, or `false` can be a meaningful override. A settings object should make its default and override semantics clear to callers.

```text
host configuration -> *Options --+
                                +-> owning adapter -> validated effective settings
runtime caller ----> *Settings -+
```

`FileSigningKeyProvider` loads `private-key.pem` and `public-key.pem` from `Jwt:KeyFolder`, generating both if either is missing. It holds them for its lifetime; there is no rotation protocol. Persist and provision a consistent pair across API instances. A fresh instance needs write access to generate them; the example Compose key mount is read-only and therefore needs pre-provisioned files. Do not commit keys or real connection credentials.

## 10. Database and container deployment

DbMigrator applies versioned migrations from `Infrastructure/Repositories/Migrations` and exits: 0 for success, 1 for failure, and 2 for invalid arguments. WebApi never initializes or upgrades the schema at startup. The design-time factory uses a placeholder PostgreSQL connection to construct the model without loading WebApi configuration.

The intended deployment order is:

1. Provision PostgreSQL, Redis, configuration, and signing keys.
2. Generate/review migration SQL or build the migrator; select the target connection explicitly.
3. Run DbMigrator and require a successful exit before starting/upgrading WebApi.
4. Complete account-role assignments and permission snapshot provisioning appropriate to the application; these are not supplied by the migration host.

The initial migration seeds built-in users, roles, and the root permission. It does not seed user-role assignments or Redis snapshots. Seeded identities/credential material are template data requiring deployment-specific handling.

`--script <path>` generates idempotent SQL without connecting to a database. Applying migrations uses `ConnectionStrings__Postgres` and only applies pending migrations. Full commands, including `dotnet ef migrations add` and standalone container execution, are maintained in the [database migration guide](README.en.md#database-migrations).

Migration files are versioned EF Core generator output. The root `.editorconfig` marks `Infrastructure/Repositories/Migrations` as generated and disables its style transformations, so the standard `dotnet format .` command preserves Migration, Designer, and model-snapshot source shape. Create model changes through `dotnet ef migrations add`; edit a migration only when the database change itself requires it.

WebApi and DbMigrator have separate Dockerfiles. [deployment/docker-compose.yaml](../deployment/docker-compose.yaml) includes PostgreSQL, Redis, MQTT, SFTP, proxy, WebApi, and a `migration` profile. Only the migrator explicitly waits for PostgreSQL health; the sample does not gate WebApi startup on migration completion. WebApi/proxy images are placeholders. MQTT/SFTP containers are deployment examples without corresponding active application integrations, and container rendering/font behavior still requires runtime verification.

## 11. Test coverage and verification boundaries

Tests use xUnit, Moq, ASP.NET Core TestHost, and local doubles under `Tests/Support`. Existing tests exercise these contracts:

| Test class | Covered behavior |
| --- | --- |
| `ArchitectureBoundaryTests` | Application assembly isolation from Infrastructure/Redis/JWT/rendering; migration presence/model alignment; offline SQL generation |
| `AuthorizationTests` | Real controllers in an in-memory test host, fallback authentication, exact/parent permissions, invalid claims, multiple roles, missing roles, logout, and invalidated tokens |
| `ClaimsPrincipalExtensionTests` | Shared user/role claim parsing and `ICurrentUser` projection, including invalid and empty identifiers |
| `UserSessionTests` | Replacement login, token-specific logout, post-save revocation, invalid role assignment, and failed-save ordering |
| `UserRegistrationTests` | Shared registration/create initialization and side-effect-free credential/role mapping |
| `PermissionCacheTests` | Read-only snapshot lookup, missing/partial snapshots, and existing empty permission sets |
| `ExpressionFingerprintTests` / `RedisCacheQueryTests` | Stable predicate fingerprints, captured values, operator whitelist, method/unlisted-operator bypass, bounded collection enforcement, and filtered-list cache miss/hit behavior |
| `SigningKeyTests` | Token validation after key generation and reload using temporary files |

Run `dotnet test Org.Product.sln`; narrow a run with `--filter FullyQualifiedName~ArchitectureBoundaryTests` when appropriate. No coverage percentage is configured.

These tests do not establish live PostgreSQL persistence, real Redis concurrency/Lua execution, successful deployment migrations, container startup, or full WebApi startup configuration. The HTTP tests assemble their own test host, so they are not an end-to-end exercise of `WebApi/Program.cs`. Claims about those behaviors require separate runtime evidence.

## 12. Extending the template

- Add HTTP routes/policies in WebApi, use-case orchestration and DTOs in Application, domain behavior/contracts in Domain, and external implementations in Infrastructure. Application services, mapping profiles, MediatR handlers, and parameterless Autofac modules in the WebApi/Application assemblies are discovered automatically. Use framework `AddXxx` registrations in the executable host before writing equivalent container code. For application-specific adapters, extend an existing semantic registration root (`IBaseService`, `IDomainService`, `ISecurityStore`, or `ISecurityService`) when its lifecycle matches; add a new root only when it represents a real capability category. Generic contracts and third-party client factories remain explicit inside the responsible module. Each executable host remains the composition root for the implementations it consumes.
- Keep transaction ownership explicit. New cross-store side effects need a defined failure/recovery strategy; an interface alone does not provide atomicity.
- For model changes, add a migration and update the model snapshot. For authorization changes, trace database roles, JWT claims, projected permission codes, and controller policy names together.
- Preserve DTO convention/manual-map boundaries and add behavior-focused tests at the layer being changed. Do not expose placeholder methods as completed capabilities.
- `.template.config/template.json` defines `org-product-web` and replaces the `Org.Product` naming token. Build solution-listed projects; legacy `*Backup*` files are excluded from template generation. Update this document when dependency direction, configuration ownership, or runtime behavior changes.
