# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first (cached layer) — only project/props files are copied.
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/TrackManagement.Domain/*.csproj src/TrackManagement.Domain/
COPY src/TrackManagement.Application/*.csproj src/TrackManagement.Application/
COPY src/TrackManagement.Infrastructure/*.csproj src/TrackManagement.Infrastructure/
COPY src/TrackManagement.Api/*.csproj src/TrackManagement.Api/
RUN dotnet restore src/TrackManagement.Api/TrackManagement.Api.csproj

COPY src/ src/
RUN dotnet publish src/TrackManagement.Api/TrackManagement.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
# Built-in non-root user in the official .NET images.
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "TrackManagement.Api.dll"]
