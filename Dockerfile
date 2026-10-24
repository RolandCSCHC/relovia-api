FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/Relovia.Api/Relovia.Api.csproj src/Relovia.Api/
RUN dotnet restore src/Relovia.Api/Relovia.Api.csproj

COPY src/Relovia.Api/ src/Relovia.Api/
RUN dotnet publish src/Relovia.Api/Relovia.Api.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080
ENV Storage__Local__Root=/var/lib/relovia/files
EXPOSE 8080

COPY --from=build /app/publish .

USER root
RUN mkdir -p /var/lib/relovia/files && chown "$APP_UID" /var/lib/relovia/files
USER $APP_UID
ENTRYPOINT ["dotnet", "Relovia.Api.dll"]
