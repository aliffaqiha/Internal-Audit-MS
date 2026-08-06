# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/Backend/IAMS.slnx ./IAMS.slnx
COPY src/Backend/Domain/IAMS.Domain.csproj ./src/Backend/Domain/
COPY src/Backend/Application/IAMS.Application.csproj ./src/Backend/Application/
COPY src/Backend/Infrastructure/IAMS.Infrastructure.csproj ./src/Backend/Infrastructure/
COPY src/Backend/API/IAMS.Api.csproj ./src/Backend/API/

RUN dotnet restore ./IAMS.slnx

COPY src/Backend/ ./src/Backend/
RUN dotnet publish src/Backend/API/IAMS.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "IAMS.Api.dll"]