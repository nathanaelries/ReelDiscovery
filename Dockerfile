# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore first so layer caching survives source-only changes
COPY src/ReelDiscovery.Core/ReelDiscovery.Core.csproj src/ReelDiscovery.Core/
COPY src/ReelDiscovery.Web/ReelDiscovery.Web.csproj src/ReelDiscovery.Web/
RUN dotnet restore src/ReelDiscovery.Web/ReelDiscovery.Web.csproj

COPY src/ src/
RUN dotnet publish src/ReelDiscovery.Web/ReelDiscovery.Web.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080 \
    REELDISCOVERY_DATA=/data

RUN mkdir -p /data && chown app:app /data
VOLUME /data
EXPOSE 8080
USER app

ENTRYPOINT ["dotnet", "ReelDiscovery.Web.dll"]
