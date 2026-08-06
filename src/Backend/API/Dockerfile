# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore with a mirrored layout so ProjectReference paths resolve.
COPY src/Backend/API/IAMS.Api.csproj ./src/Backend/API/
COPY src/Backend/Application/IAMS.Application.csproj ./src/Backend/Application/
COPY src/Backend/Infrastructure/IAMS.Infrastructure.csproj ./src/Backend/Infrastructure/
COPY src/Backend/Domain/IAMS.Domain.csproj ./src/Backend/Domain/

RUN dotnet restore ./src/Backend/API/IAMS.Api.csproj

COPY src/Backend/ ./src/Backend/
RUN dotnet publish ./src/Backend/API/IAMS.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "IAMS.Api.dll"]