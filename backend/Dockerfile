# ── Build Stage ───────────────────────────────────────────────
# .NET 10 images — must match <TargetFramework>net10.0</TargetFramework>
# in LearnPath.API.csproj.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY *.csproj ./
RUN dotnet restore LearnPath.API.csproj

COPY . ./
RUN dotnet publish LearnPath.API.csproj -c Release -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# ── Runtime Stage ─────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# curl is used only by the container health check for /api/v1/health.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

RUN groupadd --system appgroup && \
    useradd --system --gid appgroup appuser

COPY --from=build /app/publish ./

# Logs and uploads are written relative to the working directory.
RUN mkdir -p /app/Logs /app/uploads && chown -R appuser:appgroup /app
USER appuser

ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 5000

HEALTHCHECK --interval=15s --timeout=5s --start-period=60s --retries=5 \
    CMD curl -fsS http://localhost:5000/api/v1/health || exit 1

ENTRYPOINT ["dotnet", "LearnPath.API.dll"]
