# Multi-stage build for Blizka.Host — the API itself (Dockerfile.migrator is a separate,
# one-shot image that only applies EF Core migrations, see docker-compose.yml).

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# SixLabors.ImageSharp (Blizka.App, T-3.1 фото) требует лицензию под Six Labors Split License при
# Release-сборке (`dotnet build`/Debug только предупреждает, `dotnet publish -c Release` падает с error) —
# MSBuild подхватывает переменную окружения SixLaborsLicenseKey как одноимённое свойство напрямую, без
# правок в csproj/Directory.Build.props. Бесплатный community-ключ регистрируется на sixlabors.com/pricing
# (см. docs/deployment/railway.md); в CI и на Railway передаётся как build-time секрет.
ARG SixLaborsLicenseKey=""
ENV SixLaborsLicenseKey=$SixLaborsLicenseKey

COPY Directory.Build.props Directory.Packages.props ./
COPY src/ src/

RUN dotnet restore src/Blizka.Host/Blizka.Host.csproj

RUN dotnet publish src/Blizka.Host/Blizka.Host.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true

COPY --from=build /app/publish .

# Railway задаёт свой $PORT (переменную окружения) и ждёт, что контейнер слушает именно его — Program.cs
# читает PORT и переопределяет биндинг через UseUrls(), которая приоритетнее ASPNETCORE_URLS. Значение
# ниже — это дефолт для запуска контейнера вне Railway (например, локально), когда PORT не задан.
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Blizka.Host.dll"]
