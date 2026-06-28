# ── Build Stage ───────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY *.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish -c Release -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# ── Runtime Stage ─────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN addgroup --system appgroup && \
    adduser --system --ingroup appgroup appuser

COPY --from=build /app/publish ./

RUN chown -R appuser:appgroup /app
USER appuser

ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 5000

ENTRYPOINT ["dotnet", "LearnPath.API.dll"]
