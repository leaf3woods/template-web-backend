# Layer boundaries

## Project dependencies

- WebApi -> Application and Infrastructure: HTTP endpoints and composition.
- DbMigrator -> Infrastructure: database deployment configuration, execution, and exit code.
- Infrastructure -> Application and Domain: adapters implement inner-layer contracts.
- Application -> Domain and Domain.Shared: use cases and their required capabilities.
- Domain -> Domain.Shared: domain entities, repository contracts, and commit boundaries.

## Application capabilities

`Application/Abstractions/Security` defines session storage, captcha storage/generation, password credential processing, role permission storage, and token issuance. Redis, JWT, key files, and SkiaSharp implementations live in `Infrastructure/Adapters/Security`.

The role permission store represents stored permission snapshots. It does not claim to check database role existence or rebuild missing snapshots. This refactor preserves the existing authorization behavior.

Binary device frames, CRC operations, and protocol JSON models live in `Infrastructure/Adapters/Protocols`. Domain does not implement wire formats or manage key files.

## Persistence

`IRepository<T>` and `IUnitOfWork` remain in Domain. The unit of work owns saving and explicit transactions; `ISqlExecutor` is an Application persistence capability implemented in Infrastructure. The repository, SQL executor, and unit of work use the same scoped DbContext.

The current query boundary deliberately retains IQueryable and EF asynchronous query operations in Application. Intent-specific query services can be added when query complexity warrants them.

Each public mutation use case owns its commit. Compose repository operations within a use case instead of chaining independently committing application services. Generic CRUD remains available for simple resources. User creation and registration share one explicit initialization path; mapping profiles only transform data and do not assign roles or initialize credentials.

## Database deployment

DbMigrator applies migrations before the API starts. WebApi only registers persistence services. Database initialization and upgrades no longer execute in the API startup path.

See [DbMigrator](../src/Org.Product.DbMigrator/README.md) for migration generation, SQL review, and execution.
