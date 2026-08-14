# syntax=docker/dockerfile:1

# Build stage — only the Api project and its three project references (Domain,
# Application, Infrastructure) are needed; tests/ is never copied into the image.
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy just the .csproj files first so `dotnet restore` is cached as its own Docker
# layer and only re-runs when a dependency actually changes, not on every code edit.
COPY src/CommunityIncidentReporting.Domain/CommunityIncidentReporting.Domain.csproj src/CommunityIncidentReporting.Domain/
COPY src/CommunityIncidentReporting.Application/CommunityIncidentReporting.Application.csproj src/CommunityIncidentReporting.Application/
COPY src/CommunityIncidentReporting.Infrastructure/CommunityIncidentReporting.Infrastructure.csproj src/CommunityIncidentReporting.Infrastructure/
COPY src/CommunityIncidentReporting.Api/CommunityIncidentReporting.Api.csproj src/CommunityIncidentReporting.Api/
RUN dotnet restore src/CommunityIncidentReporting.Api/CommunityIncidentReporting.Api.csproj

COPY src/ src/
RUN dotnet publish src/CommunityIncidentReporting.Api/CommunityIncidentReporting.Api.csproj \
    -c Release -o /app/publish --no-restore

# Runtime stage — the official ASP.NET Core image already runs as a non-root "app"
# user by default (since .NET 8), so no extra USER directive is needed here.
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Safe default if a host doesn't set ASPNETCORE_ENVIRONMENT explicitly — never fall
# back to Development in a deployed container (that would enable Swagger and the
# auto-migrate/seed startup hook, neither of which belong in production).
ENV ASPNETCORE_ENVIRONMENT=Production

# Informational only — Program.cs binds to the platform's $PORT at runtime (Render
# assigns one dynamically), so this isn't the actual source of truth for the port.
EXPOSE 8080

ENTRYPOINT ["dotnet", "CommunityIncidentReporting.Api.dll"]
