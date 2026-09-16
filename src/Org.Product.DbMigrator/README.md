# Database migrator

`Org.Product.DbMigrator` is a one-shot console process. It applies versioned EF Core migrations and exits with code 0 on success, 1 on failure, or 2 for invalid arguments. It references Infrastructure directly and does not start WebApi, Redis, JWT signing, or captcha services.

## Generate SQL for review

From the repository root:

```powershell
dotnet run --project src/Org.Product.DbMigrator/Org.Product.DbMigrator.csproj -- --script migration.sql
```

This command does not connect to a database. The script checks migration history before applying each migration.

## Apply migrations

Supply the target connection string explicitly, preferably through the deployment secret store:

```powershell
$env:ConnectionStrings__Postgres = 'Host=localhost;Database=org_product;Username=migrator;Password=<password>'
dotnet run --project src/Org.Product.DbMigrator/Org.Product.DbMigrator.csproj
```

The process applies only pending migrations. Re-running it against an up-to-date database does not insert the initial data again. Run it before starting or upgrading WebApi; the API no longer creates or migrates schemas at startup.

The initial migration creates the existing model and its built-in users, roles, and root permission. It does not populate Redis permission snapshots or add user-role assignments.

## Add a migration

```powershell
dotnet tool restore
dotnet ef migrations add DescribeChange --project src/Org.Product.Infrastructure/Org.Product.Infrastructure.csproj --startup-project src/Org.Product.DbMigrator/Org.Product.DbMigrator.csproj --output-dir Repositories/Migrations
```

The design-time factory uses a placeholder connection string to build the model. It does not load WebApi configuration or secrets. Apply migrations with this console process; the factory's placeholder is not the runtime target.

## Container

```powershell
docker build -f src/Org.Product.DbMigrator/Dockerfile -t org-product-dbmigrator .
docker run --rm -e ConnectionStrings__Postgres org-product-dbmigrator
```

In the sample deployment compose file, the `dbmigrator` service is enabled by the `migration` profile:

```powershell
$env:ConnectionStrings__Postgres = 'Host=database;Database=org_product;Username=dev;Password=<password>'
docker compose -f deployment/docker-compose.yaml --profile migration run --build --rm dbmigrator
```

Use the same database as WebApi. A migration identity can have schema-change permissions while the normal API identity has only the permissions required for application work.
