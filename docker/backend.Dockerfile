# Build and run CommunityIncidentReporting.Api
# Build context must be the backend/ directory:
#   docker build -f docker/backend.Dockerfile -t cirs-api ./backend

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY CommunityIncidentReporting.sln ./
COPY src/CommunityIncidentReporting.Api/CommunityIncidentReporting.Api.csproj src/CommunityIncidentReporting.Api/
COPY src/CommunityIncidentReporting.Application/CommunityIncidentReporting.Application.csproj src/CommunityIncidentReporting.Application/
COPY src/CommunityIncidentReporting.Domain/CommunityIncidentReporting.Domain.csproj src/CommunityIncidentReporting.Domain/
COPY src/CommunityIncidentReporting.Infrastructure/CommunityIncidentReporting.Infrastructure.csproj src/CommunityIncidentReporting.Infrastructure/
RUN dotnet restore src/CommunityIncidentReporting.Api/CommunityIncidentReporting.Api.csproj

COPY src/ src/
RUN dotnet publish src/CommunityIncidentReporting.Api/CommunityIncidentReporting.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app ./
ENTRYPOINT ["dotnet", "CommunityIncidentReporting.Api.dll"]
