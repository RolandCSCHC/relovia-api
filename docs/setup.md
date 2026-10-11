# Setup

## Prerequisites

- .NET SDK 10. `global.json` requests 10.0.100 and rolls forward to the latest 10.0 patch.
- Docker, for local PostgreSQL and the integration tests.
- A Google OAuth client if you want to sign in. The API starts without one. `GET /api/auth/google` then returns 503.

## Database

From the repository root:

```bash
docker compose up -d
```

Postgres listens on host port 5433. Port 5432 is left free for another local Postgres install. The container user, password, and database name are `relovia`. The volume uses PostgreSQL 18's data directory, `/var/lib/postgresql`.

The Development connection string is already in `src/Relovia.Api/appsettings.Development.json`:

```text
Host=localhost;Port=5433;Database=relovia;Username=relovia;Password=relovia;SSL Mode=Disable
```

Apply migrations. Restore the local `dotnet-ef` tool from the repository root, then run the update from `src/Relovia.Api`. The design-time factory searches upward from the current directory for that project.

```bash
dotnet tool restore
cd src/Relovia.Api
dotnet ef database update
```

## Google sign-in

The user-secrets id is `relovia-api`. From `src/Relovia.Api`:

```bash
dotnet user-secrets set "Authentication:Google:ClientId" "<client-id>"
dotnet user-secrets set "Authentication:Google:ClientSecret" "<client-secret>"
```

In the Google client, set the authorized redirect URI to:

```text
http://localhost:5080/api/auth/google/callback
```

Do not commit the client secret. Production should supply the same keys as environment variables.

## Run

From the repository root:

```bash
dotnet run --project src/Relovia.Api --launch-profile http
```

The `http` profile listens on `http://localhost:5080`. The Angular app expects that address.

- `GET /` returns the service name.
- `GET /health` checks the database.
- In Development, the OpenAPI document is `/openapi/v1.json` and the Scalar reference is `/scalar/v1`.

The `https` profile also listens on `https://localhost:7080`. Local sign-in uses the `http` profile.

## Configuration

ASP.NET Core configuration. Environment variables use `__` between nested keys, for example `ConnectionStrings__Relovia`.

| Key | Required | Purpose |
| --- | --- | --- |
| `ConnectionStrings:Relovia` | Yes | Postgres connection string. Local Development disables SSL. A hosted database should require it. |
| `Web:Origins` | Yes | Browser origins allowed to call the API with credentials. The first origin is where Google sign-in redirects after success. Development lists `http://localhost:4200`. |
| `Web:RequestTokenCookieDomain` | No | Leave empty so `XSRF-TOKEN` stays on the API host. Set a parent domain, such as `.example.com`, only when the app and API are sibling subdomains. |
| `Authentication:Google:ClientId` | For Google sign-in | OAuth client id. |
| `Authentication:Google:ClientSecret` | For Google sign-in | OAuth client secret. |
| `Session:SlidingLifetime` | No | Default `14.00:00:00`. Must be positive. |
| `Session:AbsoluteLifetime` | No | Default `30.00:00:00`. Must cover the sliding lifetime. |
| `ASPNETCORE_ENVIRONMENT` | No | `Development` turns on user secrets, OpenAPI, and Scalar. |
| `ASPNETCORE_HTTP_PORTS` | No | Listen ports. The image sets `8080`. |
| `PORT` | No | When set, replaces `ASPNETCORE_HTTP_PORTS`. Some container hosts inject this. |

Keep hosted database URLs and Google secrets out of Git.

## Tests

```bash
dotnet test Relovia.sln
```

Unit tests do not need Postgres. Integration tests start a `postgres:18` container, so Docker must be running.

## Container image

```bash
docker build --tag relovia-api .
```

The image publishes `src/Relovia.Api` only. It runs as the non-root user from the ASP.NET runtime image.

## CI

GitHub Actions runs on pushes to `main` and on pull requests. One job runs `dotnet test`. Another builds the container image.
