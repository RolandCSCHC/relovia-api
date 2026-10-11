# relovia-api

HTTP API for Relovia. It owns identity, the database, and Google sign-in. The Angular app never receives a Google access token, refresh token, or ID token.

Setup, configuration, and local commands are in [docs/setup.md](docs/setup.md).

## Stack

| Piece | Choice |
| --- | --- |
| Runtime | .NET 10, ASP.NET Core |
| Data | EF Core 10, Npgsql, PostgreSQL 18 |
| Auth | Google OpenID Connect, cookie session, antiforgery |
| API docs | OpenAPI and Scalar, Development only |
| Tests | xUnit. Integration tests use Testcontainers and `postgres:18` |
| Host | Portable Dockerfile. Listens on 8080, or on `PORT` when a host sets it |

`global.json` requests SDK 10.0.100 and rolls forward to the latest 10.0 patch.

## Shape

One project, `src/Relovia.Api`. Feature code lives under `Features`. Cross-cutting code lives under `Common`. Controllers stay thin.

There is no separate Domain, Application, or Infrastructure project, no generic repository, and no ASP.NET Core Identity. `ReloviaDbContext` is the data access layer. Business rules live in feature services.

Current feature: `Features/Identity` (`User`, `ExternalLogin`, `AuthSession`).

## Persistence

- EF Core migrations only. The API does not migrate on startup.
- Snake_case column and table names.
- UUID v7 primary keys.
- Timestamps are `timestamptz`, written at microsecond precision so a reload matches the saved value.
- Delete behavior is set on each relationship. The current foreign keys use `Restrict`.
- Email is stored trimmed and lowercased.

## Authentication

Google OpenID Connect runs here. The callback reads the profile and discards the Google tokens.

The browser session is the HttpOnly cookie `relovia_session`. The cookie value is a random token. Postgres stores only a hash of it. Logout sets `revoked_at` and clears the cookie, so a copied cookie stops working. A session has a sliding lifetime and an absolute lifetime, so it cannot be refreshed forever.

The cookie is `SameSite=Lax` and host-only. It is `Secure` outside local HTTP. Localhost ports are the same site, so the Angular app on port 4200 sends the cookie to this API on port 5080.

Mutating requests require the header `X-XSRF-TOKEN`. The readable request cookie is `XSRF-TOKEN`. The secret cookie is `relovia_antiforgery`. Data Protection keys are stored in Postgres, so a redeploy can still read protected cookies.

Login identity is Google's `sub`, stored on `ExternalLogin` with a unique `(provider, provider_subject)`. A matching email with a different `sub` is not linked automatically.

## HTTP

Unexpected failures and validation errors use Problem Details. Outside Development, logs are single-line JSON. `GET /health` checks the database. `GET /` returns the service name.

Endpoints require an authenticated session unless they opt out. `GET /api/auth/google` starts sign-in, `GET /api/auth/me` returns the current user, and `POST /api/auth/logout` revokes the session.
