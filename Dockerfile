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
EXPOSE 8080

COPY --from=build /app/publish .

USER $APP_UID
ENTRYPOINT ["dotnet", "Relovia.Api.dll"]
