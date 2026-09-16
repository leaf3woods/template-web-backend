FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/Org.Product.DbMigrator/Org.Product.DbMigrator.csproj src/Org.Product.DbMigrator/
COPY src/Org.Product.Infrastructure/Org.Product.Infrastructure.csproj src/Org.Product.Infrastructure/
COPY src/Org.Product.Application/Org.Product.Application.csproj src/Org.Product.Application/
COPY src/Org.Product.Domain/Org.Product.Domain.csproj src/Org.Product.Domain/
COPY src/Org.Product.Domain.Shared/Org.Product.Domain.Shared.csproj src/Org.Product.Domain.Shared/
RUN dotnet restore src/Org.Product.DbMigrator/Org.Product.DbMigrator.csproj
COPY src/ src/
RUN dotnet publish src/Org.Product.DbMigrator/Org.Product.DbMigrator.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
USER app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Org.Product.DbMigrator.dll"]
