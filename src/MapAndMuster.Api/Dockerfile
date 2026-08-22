# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY .editorconfig global.json Directory.Build.props Directory.Packages.props MapAndMuster.sln ./
COPY src/MapAndMuster.Domain/MapAndMuster.Domain.csproj src/MapAndMuster.Domain/
COPY src/MapAndMuster.Application/MapAndMuster.Application.csproj src/MapAndMuster.Application/
COPY src/MapAndMuster.Infrastructure/MapAndMuster.Infrastructure.csproj src/MapAndMuster.Infrastructure/
COPY src/MapAndMuster.Api/MapAndMuster.Api.csproj src/MapAndMuster.Api/

RUN dotnet restore src/MapAndMuster.Api/MapAndMuster.Api.csproj

COPY src/MapAndMuster.Domain/ src/MapAndMuster.Domain/
COPY src/MapAndMuster.Application/ src/MapAndMuster.Application/
COPY src/MapAndMuster.Infrastructure/ src/MapAndMuster.Infrastructure/
COPY src/MapAndMuster.Api/ src/MapAndMuster.Api/

RUN dotnet publish src/MapAndMuster.Api/MapAndMuster.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
COPY src/MapAndMuster.Api/docker-entrypoint.sh /app/docker-entrypoint.sh
RUN chmod +x /app/docker-entrypoint.sh \
    && mkdir -p /app/app-data \
    && chown -R $APP_UID:$APP_UID /app
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080
ENV Storage__RootPath=/app/app-data
EXPOSE 8080
ENTRYPOINT ["/app/docker-entrypoint.sh"]
