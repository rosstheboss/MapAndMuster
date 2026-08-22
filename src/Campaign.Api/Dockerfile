# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY .editorconfig global.json Directory.Build.props Directory.Packages.props Campaign.sln ./
COPY src/Campaign.Domain/Campaign.Domain.csproj src/Campaign.Domain/
COPY src/Campaign.Application/Campaign.Application.csproj src/Campaign.Application/
COPY src/Campaign.Infrastructure/Campaign.Infrastructure.csproj src/Campaign.Infrastructure/
COPY src/Campaign.Api/Campaign.Api.csproj src/Campaign.Api/

RUN dotnet restore src/Campaign.Api/Campaign.Api.csproj

COPY src/Campaign.Domain/ src/Campaign.Domain/
COPY src/Campaign.Application/ src/Campaign.Application/
COPY src/Campaign.Infrastructure/ src/Campaign.Infrastructure/
COPY src/Campaign.Api/ src/Campaign.Api/

RUN dotnet publish src/Campaign.Api/Campaign.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
COPY src/Campaign.Api/docker-entrypoint.sh /app/docker-entrypoint.sh
RUN chmod +x /app/docker-entrypoint.sh \
    && mkdir -p /app/app-data \
    && chown -R $APP_UID:$APP_UID /app
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080
ENV Storage__RootPath=/app/app-data
EXPOSE 8080
ENTRYPOINT ["/app/docker-entrypoint.sh"]
