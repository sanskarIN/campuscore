FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore CampusCore.sln
RUN dotnet publish src/CampusCore.Api/CampusCore.Api.csproj --configuration Release --no-restore --output /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_EnableDiagnostics=0
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/* \
    && addgroup --system campuscore \
    && adduser --system --ingroup campuscore campuscore
COPY --from=build --chown=campuscore:campuscore /app/publish .
USER campuscore
EXPOSE 8080
HEALTHCHECK --interval=10s --timeout=5s --start-period=15s --retries=5 CMD curl --fail --silent --show-error http://127.0.0.1:8080/healthz >/dev/null || exit 1
ENTRYPOINT ["dotnet", "CampusCore.Api.dll"]
