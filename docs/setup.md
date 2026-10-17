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

In the Google client, set both authorized redirect URIs:

```text
http://localhost:5080/api/auth/google/callback
https://api.relovia.app/api/auth/google/callback
```

Do not commit the client secret. Production supplies the same keys as environment variables. On the OAuth consent screen, authorize the domain `relovia.app`. While the app is in testing, the Google account must be listed as a test user.

## City list

Choosing a country fills its city dropdown from the GeoNames [cities1000](https://download.geonames.org/export/dump/cities1000.zip) list: populated places of at least 1,000 people. The API downloads that list the first time a city dropdown is opened and keeps it for a day. No GeoNames account is required.

## Run

From the repository root:

```bash
dotnet run --project src/Relovia.Api --launch-profile http
```

The `http` profile listens on `http://localhost:5080`. The Angular app expects that address.

- `GET /` returns the service name.
- `GET /health` checks the database.
- In Development, the OpenAPI document is `/openapi/v1.json` and the Scalar reference is `/scalar/v1`. Each operation has a stable `operationId` so the Angular client can name its methods. Enum values in that document are the member names.

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
| `Access:Restricted` | No | Default `true`. While true, only `Access:AllowedEmails` can use the API: every other signed-in user gets `403` and `hasAccess: false` from `GET /api/auth/me`. Set to `false` to open the app to everyone. |
| `Access:AllowedEmails` | No | Google accounts that can use the app while it is restricted, and that receive the "someone is interested" email. `appsettings.json` lists `rcschlosser@miuandes.cl`. Environment variables: `Access__AllowedEmails__0`, `Access__AllowedEmails__1`. |
| `Email:ApiKey` | For the interest email | [Resend](https://resend.com) API key. Without it nobody is emailed; the API logs a warning and tries again the next time that person signs in. Keep it out of Git. |
| `Email:From` | No | Sender. Default `Relovia <onboarding@resend.dev>`, which only delivers to the Resend account owner. Use an address on a verified domain to email anyone else. |
| `Session:SlidingLifetime` | No | Default `14.00:00:00`. Must be positive. |
| `Session:AbsoluteLifetime` | No | Default `30.00:00:00`. Must cover the sliding lifetime. |
| `ASPNETCORE_ENVIRONMENT` | No | `Development` turns on user secrets, OpenAPI, and Scalar. |
| `ASPNETCORE_HTTP_PORTS` | No | Listen ports. The image sets `8080`. |
| `PORT` | No | When set, replaces `ASPNETCORE_HTTP_PORTS`. Some container hosts inject this. |

Keep hosted database URLs and Google secrets out of Git.

## Production

The API runs from the Dockerfile on Render. Neon hosts Postgres 18. The public API origin is `https://api.relovia.app`. The app origin is `https://relovia.app`.

Npgsql needs a keyword connection string. A `postgresql://` URI is rejected. Use the pooled Neon host for the running API, and the direct host (the one without `-pooler`) only for migrations. The API does not migrate on startup.

```text
Host=<pooled-host>;Database=relovia;Username=relovia_owner;Password=<password>;SSL Mode=Require
```

Render environment variables:

| Variable | Value |
| --- | --- |
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ConnectionStrings__Relovia` | Pooled keyword string above |
| `Web__Origins__0` | `https://relovia.app` |
| `Web__RequestTokenCookieDomain` | `.relovia.app` |
| `Authentication__Google__ClientId` | OAuth web client id |
| `Authentication__Google__ClientSecret` | OAuth web client secret |

Add the Resend key so the developer is emailed when someone signs in to show interest:

| Variable | Value |
| --- | --- |
| `Email__ApiKey` | Resend API key |

Email goes to Resend over HTTPS, not SMTP: Render's free tier blocks outbound SMTP ports, so a connection to a mail server just times out. Create the Resend account with an address listed in `Access:AllowedEmails`. Resend's test sender (`onboarding@resend.dev`, the default) can only deliver to the address that owns the account.

`GET /health` checks the database. Changing an environment variable takes effect after a new deploy.

### Closed preview

Until the app is ready, production is restricted (`Access:Restricted` defaults to `true`). Anyone can sign in with Google, but only the emails in `Access:AllowedEmails` can use the app. The first time someone else signs in, the API emails the allowed addresses once (`users.interest_notified_at` records it). A failed or unconfigured email never blocks sign-in, and is retried on that person's next sign-in.

To let another person in, add their email as `Access__AllowedEmails__1` and redeploy. To open the app to everyone, set `Access__Restricted` to `false`.

### Production migrations

The `Migrate production database` job in `.github/workflows/ci.yml` runs `dotnet ef database update` against Neon on every push to `main`, after the tests pass. Pull requests never touch production. Already-applied migrations are skipped, so a run with nothing pending changes nothing.

One-time setup: add a repository secret named `MIGRATION_CONNECTION_STRING` (GitHub: Settings, Secrets and variables, Actions). The value is a keyword connection string for the Neon **direct** host, the one without `-pooler`:

```text
Host=<direct-host>;Database=relovia;Username=relovia_owner;Password=<password>;SSL Mode=Require
```

If the secret is missing the job fails with a message saying so.

### Production deploys

The `Deploy to Render` job runs after `Migrate production database` and calls the Render deploy hook, so new code never starts before its schema exists. Render's own "After CI Checks Pass" setting is not used: it waits for every GitHub check suite on the commit, and an app that leaves a suite queued forever (DigitalOcean, GitGuardian) blocks the deploy.

One-time setup:

1. In Render, open the service, Settings, Deploy, and copy the **Deploy Hook** URL.
2. Add it as a repository secret named `RENDER_DEPLOY_HOOK` (GitHub: Settings, Secrets and variables, Actions). Treat it like a password, because anyone with the URL can trigger a deploy.
3. In the same Render section, set **Auto-Deploy** to **Off**. Otherwise Render also deploys on its own and the migration ordering is lost.

If the secret is missing the job fails with a message saying so, after the migrations have already run. Write migrations so the previous API version still works against the new schema (add before you remove) so a failed or slow deploy never leaves production broken.

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

GitHub Actions runs on pushes to `main` and on pull requests. The Test job runs `dotnet test`. The Image job builds the container image after the tests pass.
