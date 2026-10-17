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

Current features: `Features/Identity` (`User`, `ExternalLogin`, `AuthSession`) and `Features/Relocations` (`Relocation`, `ChecklistTemplate`, `TaskTemplate`, `RelocationTask`, `TaskDependency`), `Features/Budgets` (`Budget`, `BudgetCategory`, `BudgetEstimate`, `Expense`), and `Features/ExchangeRates` (rate provider and cache).

## Persistence

- EF Core migrations only. The API does not migrate on startup. CI applies pending migrations to production on every push to `main` (see `docs/setup.md`).
- Snake_case column and table names.
- UUID v7 primary keys.
- Timestamps are `timestamptz`, written at microsecond precision so a reload matches the saved value. Calendar dates are `date`.
- Enums are stored as their names.
- Delete behavior is set on each relationship. User foreign keys use `Restrict`. A draft template's tasks and dependency codes are deleted with the template. A confirmed task's `source_template_task_id` uses `ON DELETE SET NULL`. `task_dependencies` stores `relocation_id` and composite foreign keys to both tasks.
- Email is stored trimmed and lowercased.
- A relocation stores origin and destination country codes as uppercase ISO 3166-1 alpha-2. Domestic versus international is derived by comparing those codes and is not a column.
- A checklist template is identified by a stable `code` and a `version`. Task `code`s are unique inside that template, and each task records the codes it depends on. A published version cannot be changed. Priority is `Low`, `Normal`, or `High`.
- Version 1 of `domestic-core` and `international-core` is published in the migrations. Neither sets a destination country or a purpose, so selection can match any move of that scope.
- Selection keeps published templates whose scope is the move's derived type or `Both`. A destination country or purpose on the template applies only when it is set. Each template code contributes its highest published version. Tasks that share a code become one task: a set destination country outranks a set purpose, either outranks a template with both unset, and a tie keeps the template code that sorts first. Dependency codes from the copies are combined, and a code with no selected task is dropped.
- A budget belongs to one relocation (`budgets.relocation_id` is the key) and has a three-letter currency code. Categories, estimates, and expenses reference `(relocation_id, category_id)`, so they cannot point at another relocation's category. Deleting a relocation removes its budget, categories, estimates, and expenses. An estimate goes with its category; a category that still has expenses cannot be deleted. An expense's `task_id` uses `ON DELETE SET NULL`, and the service checks that the task belongs to the same relocation.
- Money is `numeric(18,4)`, always greater than zero. Estimates and expenses each store the amount and currency the user entered, and that original is never overwritten. Converted amounts are computed on read and not stored.

## Authentication

Google OpenID Connect runs here. The callback reads the profile and discards the Google tokens.

The browser session is the HttpOnly cookie `relovia_session`. The cookie value is a random token. Postgres stores only a hash of it. Logout sets `revoked_at` and clears the cookie, so a copied cookie stops working. A session has a sliding lifetime and an absolute lifetime, so it cannot be refreshed forever.

The cookie is `SameSite=Lax` and host-only. It is `Secure` outside local HTTP. Localhost ports are the same site, so the Angular app on port 4200 sends the cookie to this API on port 5080. In production, `relovia.app` and `api.relovia.app` are the same site, so the browser sends that cookie. `XSRF-TOKEN` uses the parent domain `.relovia.app` so the app host can read it.

Mutating requests require the header `X-XSRF-TOKEN`. The readable request cookie is `XSRF-TOKEN`. The secret cookie is `relovia_antiforgery`. Data Protection keys are stored in Postgres, so a redeploy can still read protected cookies.

Login identity is Google's `sub`, stored on `ExternalLogin` with a unique `(provider, provider_subject)`. A matching email with a different `sub` is not linked automatically.

## HTTP

Unexpected failures and validation errors use Problem Details. Outside Development, logs are single-line JSON. `GET /health` checks the database. `GET /` returns the service name.

Endpoints require an authenticated session unless they opt out. `GET /api/auth/google` starts sign-in, `GET /api/auth/me` returns the current user, and `POST /api/auth/logout` revokes the session.

`GET /api/places/cities?country=DE` lists city names for a two-letter country code. The names are populated places of at least 1,000 people from the GeoNames cities1000 list. `/api/relocations` lists, reads, creates, updates, and deletes relocations for the signed-in user. Another user's relocation is not found. Purpose and status are the names `Work`, `Study`, `Family`, `Lifestyle`, `Other` and `Draft`, `Active`, `Completed`, `Archived`. The response includes `isInternational`, which is true when the country codes differ.

`GET /api/relocations/{id}/checklist` returns the suggested tasks and does not write them. When the relocation has a target date, the due date is that date minus `daysBeforeMove`. Destination city and target date can be left empty, and the due date stays empty until a target date is saved. `POST` confirms the tasks the caller kept. A code must come from that suggestion; an omitted task is left out, and a dependency that points at an omitted task is dropped. Title, days before the move, phase, and priority can be changed in that request, and the due date is calculated again. A second confirm is rejected once tasks exist. Self-references, links across relocations, and cycles are rejected.

`GET /api/relocations/{id}/tasks` lists those tasks and progress. Progress is `done / (todo + in progress + done)`. A dependency is satisfied when the prerequisite is `Done` or `Skipped`. `POST` adds a task that starts as `Todo`. `PUT /api/relocations/{id}/tasks/{taskId}` updates the title, due date, phase, priority, and status. `DELETE` removes that task and the dependency links that mention it. The template is left unchanged.

`GET /api/relocations/{id}/budget` returns the budget currency, each category with its estimates, and the estimate versus actual comparison in that currency: `estimated`, `actual`, and `remaining` (estimated minus actual) per category and in total. `currencyCode` is `null` until the budget is set up. `PUT` on the same path sets the currency; the first call creates the budget with starter categories. The budget currency is only what totals are shown in, so it can change at any time without altering an estimate or expense. Each estimate and expense has its own currency. Amounts in another currency are converted into the budget currency with the latest exchange rates, so past totals move when rates move. Each item carries its original `amount` and `currencyCode` plus `convertedAmount`, which is `null` when there is no rate. An amount without a rate is left out of the totals and listed per currency in `unconverted`. `ratesAsOf` is when the provider last updated the rates, and `ratesUnavailable` is true when conversions were needed and no rates could be loaded. Categories (`/budget/categories`) and estimates (`/budget/estimates`) have create, update, and delete. Category names are unique per relocation, ignoring case.

`/api/relocations/{id}/expenses` lists (newest first), creates, updates, and deletes expenses. An expense has a category, an optional task, a description, an amount, a currency, the date it was paid, and optional notes. Another user's relocation, or a category or task from a different relocation, is rejected. Each expense also returns `convertedAmount`, its value in the budget currency at the latest rate.

Exchange rates come from `open.er-api.com` (free, no key, updated once a day, latest rates only) behind `IExchangeRateProvider`. `ExchangeRateService` caches them in memory for six hours per base currency, and if a refresh fails it serves the last rates for up to seven days. Rates are only requested when something is in a currency other than the budget currency. The provider's terms ask for attribution to exchangerate-api.com wherever rates are shown, which the budget screen does not currently do. Tests replace the provider with fixed rates.

## Hosts

| | Address |
| --- | --- |
| Local API | `http://localhost:5080` |
| Local app | `http://localhost:4200` |
| Production API | `https://api.relovia.app` on Render |
| Production database | Neon Postgres 18 |
| Production app | `https://relovia.app` on Vercel |

Production configuration, including the Google redirect URI, is in [docs/setup.md](docs/setup.md).
