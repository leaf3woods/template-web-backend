#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Org.Product.WebApi/Org.Product.WebApi.csproj", "src/Org.Product.WebApi/"]
COPY ["src/Org.Product.Application/Org.Product.Application.csproj", "src/Org.Product.Application/"]
COPY ["src/Org.Product.Domain.Shared/Org.Product.Domain.Shared.csproj", "src/Org.Product.Domain.Shared/"]
COPY ["src/Org.Product.Domain/Org.Product.Domain.csproj", "src/Org.Product.Domain/"]
COPY ["src/Org.Product.Infrastructure/Org.Product.Infrastructure.csproj", "src/Org.Product.Infrastructure/"]
RUN dotnet restore "./src/Org.Product.WebApi/Org.Product.WebApi.csproj"
COPY . .
WORKDIR "/src/src/Org.Product.WebApi"
RUN dotnet build "./Org.Product.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Org.Product.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Org.Product.WebApi.dll"]
