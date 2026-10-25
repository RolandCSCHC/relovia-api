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

Current features: `Features/Identity` (`User`, `ExternalLogin`, `AuthSession`) and `Features/Relocations` (`Relocation`, `ChecklistTemplate`, `TaskTemplate`, `RelocationTask`, `TaskDependency`), `Features/Budgets` (`Budget`, `BudgetCategory`, `BudgetEstimate`, `Expense`), `Features/Inventory` (`InventoryItem`, `InventoryListTemplate`, `InventoryTemplateItem`), `Features/Documents` (`Document` and `IObjectStorage`), `Features/Calendar` (`CalendarEvent`), `Features/Notifications` (`NotificationPreference`, `Notification`, `NotificationEmail`), `Features/ExchangeRates` (rate provider and cache), and `Features/Export` (the relocation PDF, which stores nothing).

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
- An inventory item belongs to one relocation. Deleting the relocation removes its items. Category is `Furniture`, `Appliances`, `Linens`, `Cookware`, `Electronics`, `Decor`, or `Other`. Room is `Kitchen`, `Bathroom`, `Bedroom`, `LivingRoom`, `DiningRoom`, `Office`, or `Other`. Either can be left empty. Acquisition is `Owned` or `ToPurchase`. The moving action (`Bring`, `Sell`, `Donate`, `Store`, `Discard`) can be left unset. Packing status (`NotPacked`, `Packed`, `Unpacked`) is stored only when the action is `Bring`. Estimated and actual prices are optional and keep the currency they were entered in. A price on an item still to buy requires one of that relocation's budget categories, and that category cannot be deleted while the item points at it. `code` is copied from a room list and is empty when the user added the item by hand. It is unique per relocation when set. `source_template_item_id` uses `ON DELETE SET NULL`.
- A room list is identified by a stable `code` and a `version`, belongs to one room, and has scope `Furnished`, `Unfurnished`, or `Both`. Item `code`s are unique inside that list. A published version cannot be changed. Version 1 of the starter lists is published in the migrations. `Both` lists are linens, cookware, and bathroom basics. `Unfurnished` lists add furniture and appliances. Selection keeps published lists whose scope is the chosen home or `Both`, optionally limited to one room. Each list code contributes its highest published version. Items that share a code become one item, and a list for one kind of home outranks a shared list.
- A calendar event belongs to one relocation. Deleting the relocation removes its events. A milestone stores a calendar date and no clock time. An appointment stores a start time and an optional later end. Location is optional. Task due dates are not copied onto events.
- A notification preference is one row per user. Deleting the user is restricted while it exists. The channel is `None`, `InApp`, `Email`, or `Both`, and the lead time is 0 to 90 days. A reminder belongs to one user (restricted) and one relocation (`ON DELETE CASCADE`). One reminder is stored per user, kind, and source. The source id is not a foreign key, so deleting the task, event, or document leaves the reminder. An email outbox row belongs to that reminder (`ON DELETE CASCADE`) and is unique, so a retry cannot create a second message.
- A document belongs to one relocation. Deleting the relocation removes the row (`ON DELETE CASCADE`) and the stored file. The bytes live behind `IObjectStorage`. The storage key is the document id in hex, never the name the user chose, and it is not returned by the API. Development writes a private directory that is not served as static files. `Storage:Provider` = `S3` uses an S3-compatible private bucket. Production Neon is AWS US East 1 (N. Virginia), with path-style addressing and signing region `us-east-1`. Allowed files are PDF, JPEG, PNG, and WebP, at most 10 MB, recognized from the file header. Category is optional: `Identity`, `Visa`, `Housing`, `Employment`, `Education`, `Insurance`, `Health`, `Finance`, `Moving`, or `Other`. Expiration is an optional calendar date and may already have passed. A document links to tasks on that same relocation through `document_task_links`, which stores `relocation_id` and composite foreign keys. Deleting the task or the document removes the link.

## Authentication

Google OpenID Connect runs here. The callback reads the profile and discards the Google tokens.

The browser session is the HttpOnly cookie `relovia_session`. The cookie value is a random token. Postgres stores only a hash of it. Logout sets `revoked_at` and clears the cookie, so a copied cookie stops working. A session has a sliding lifetime and an absolute lifetime, so it cannot be refreshed forever.

The cookie is `SameSite=Lax` and host-only. It is `Secure` outside local HTTP. Localhost ports are the same site, so the Angular app on port 4200 sends the cookie to this API on port 5080. In production, `relovia.rolandschlosser.dev` and `api.relovia.rolandschlosser.dev` are the same site, so the browser sends that cookie. `XSRF-TOKEN` uses the parent domain `.relovia.rolandschlosser.dev` so the app host can read it.

Mutating requests require the header `X-XSRF-TOKEN`. The readable request cookie is `XSRF-TOKEN`. The secret cookie is `relovia_antiforgery`. Data Protection keys are stored in Postgres, so a redeploy can still read protected cookies.

Login identity is Google's `sub`, stored on `ExternalLogin` with a unique `(provider, provider_subject)`. A matching email with a different `sub` is not linked automatically.

## HTTP

Unexpected failures and validation errors use Problem Details. Outside Development, logs are single-line JSON. `GET /health` checks the database. `GET /` returns the service name.

Endpoints require an authenticated session unless they opt out. `GET /api/auth/google` starts sign-in, `GET /api/auth/me` returns the current user, and `POST /api/auth/logout` revokes the session.

`GET /api/places/cities?country=DE` lists city names for a two-letter country code. The names are populated places of at least 1,000 people from the GeoNames cities1000 list. `/api/relocations` lists, reads, creates, updates, and deletes relocations for the signed-in user. Another user's relocation is not found. Purpose and status are the names `Work`, `Study`, `Family`, `Lifestyle`, `Other` and `Draft`, `Active`, `Completed`, `Archived`. The response includes `isInternational`, which is true when the country codes differ.

`GET /api/relocations/{id}/checklist` returns the suggested tasks and does not write them. When the relocation has a target date, the due date is that date minus `daysBeforeMove`. Destination city and target date can be left empty, and the due date stays empty until a target date is saved. `POST` confirms the tasks the caller kept. A code must come from that suggestion; an omitted task is left out, and a dependency that points at an omitted task is dropped. Title, days before the move, phase, and priority can be changed in that request, and the due date is calculated again. A second confirm is rejected once tasks exist. Self-references, links across relocations, and cycles are rejected.

`GET /api/relocations/{id}/tasks` lists those tasks and progress. Progress is `done / (todo + in progress + done)`. A dependency is satisfied when the prerequisite is `Done` or `Skipped`. `POST` adds a task that starts as `Todo`. `PUT /api/relocations/{id}/tasks/{taskId}` updates the title, due date, phase, priority, and status. `DELETE` removes that task, the dependency links that mention it, and its document links. The template and the documents stay. Each task includes the ids of the documents linked to it.

`GET /api/relocations/{id}/budget` returns the budget currency, each category with its estimates, and the estimate versus actual comparison in that currency: `estimated`, `actual`, and `remaining` (estimated minus actual) per category and in total. Prices on inventory items that are still to buy are included in the category they point at. An owned item is not included. `shopping` on a category lists those items with their original prices and the converted amounts. `currencyCode` is `null` until the budget is set up. `PUT` on the same path sets the currency; the first call creates the budget with starter categories. The budget currency is only what totals are shown in, so it can change at any time without altering an estimate or expense. Each estimate and expense has its own currency. Amounts in another currency are converted into the budget currency with the latest exchange rates, so past totals move when rates move. Each item carries its original `amount` and `currencyCode` plus `convertedAmount`, which is `null` when there is no rate. An amount without a rate is left out of the totals and listed per currency in `unconverted`. `ratesAsOf` is when the provider last updated the rates, and `ratesUnavailable` is true when conversions were needed and no rates could be loaded. Categories (`/budget/categories`) and estimates (`/budget/estimates`) have create, update, and delete. Category names are unique per relocation, ignoring case.

`/api/relocations/{id}/expenses` lists (newest first), creates, updates, and deletes expenses. An expense has a category, an optional task, a description, an amount, a currency, the date it was paid, and optional notes. Another user's relocation, or a category or task from a different relocation, is rejected. Each expense also returns `convertedAmount`, its value in the budget currency at the latest rate.

`/api/relocations/{id}/inventory` lists, creates, updates, and deletes belongings for the signed-in user, ordered by room and then by name. Another user's relocation is not found. Category and room are the names above, or empty. A bring item with no packing status is stored as `NotPacked`. A packing status on any other action is rejected. A price requires a three-letter currency, and a currency without a price is rejected. A price on an item still to buy also requires a budget category from that relocation. The stored prices are not converted on the item; the budget converts them when it totals the category. Editing an item does not change the room list it came from.

`GET /api/relocations/{id}/inventory/suggestion?furnishing=Unfurnished&room=Kitchen` returns the suggested purchases and does not write them. `furnishing` is `Furnished` or `Unfurnished`. `room` is optional. Items already stored under the same `code` are left out. `POST` on that path confirms the codes the caller kept and writes them as things to buy, with the list's name, category, room, quantity, and priority, and no price. A code that is not on that suggestion is rejected. A code already stored is skipped. A later edit of the item leaves the list unchanged.

`GET /api/relocations/{id}/calendar` lists the relocation's milestones and appointments, and separately the tasks that have a due date. A task due date is not stored as an event. `POST /api/relocations/{id}/events` adds one, `PUT` updates it, and `DELETE` removes it. A milestone needs a date and cannot have a start or end time. An appointment needs a start time, cannot have a milestone date, and an end must be later than the start. Changing an event does not change a task, and changing or deleting a task does not change an event. Another user's relocation or event is not found. Deleting the relocation removes the events.

`GET /api/notifications` lists the signed-in user's in-app reminders, unread first. `POST /api/notifications/{id}/read` marks one, and a second call keeps the first time. `POST /api/notifications/read` marks them all. `GET /api/notifications/preferences` returns in the app and 3 days before until the user saves a preference. `PUT` on that path sets the channel and the lead time. Another user's reminder is not found. A background pass in this process creates a reminder once when a task due date, milestone, the UTC date of an appointment, or a document expiration falls on today or within the lead time. A date that has already passed is left alone. Email is written to the outbox when the channel asks for it, sent at most once, and retried up to 5 times. Changing the preference later does not mail or hide a reminder that already exists. Deleting the relocation removes its reminders and their emails.

`/api/relocations/{id}/documents` lists and uploads private files for the signed-in user, oldest first, and deletes one. Upload is multipart form data with the field `file`. A new document has no category, expiration, or task links. `PUT /api/relocations/{id}/documents/{documentId}` sets those and leaves the file unchanged. Category is one of the names above, or empty. `taskIds` replaces the previous links; a task from another relocation is rejected and the stored links stay. `GET /api/relocations/{id}/documents/{documentId}/content` returns the bytes only after checking that this user owns the relocation. The response is an attachment, `X-Content-Type-Options: nosniff`, and `Cache-Control: private, no-store`. The body does not include the storage key or a public URL. Another user's relocation or document is not found, and that request does not open the file. PDF, JPEG, PNG, and WebP up to 10 MB are accepted; the type is taken from the file header. Deleting the document or the relocation removes the stored file.

`GET /api/relocations/{id}/export` downloads that relocation as a PDF for the signed-in user. The file is drawn when the request arrives and is not stored. Optional `timeZone` is an IANA name used for appointment times; a missing or unknown zone uses UTC. The PDF contains the relocation, every task, the budget (estimates, expenses, shopping, totals, and amounts left out of the totals), the inventory, the calendar events, and the document list. Uploaded files stay in storage. `GET /api/relocations/{id}/export/summary` downloads the short summary: open tasks, money totals, what is still to pack, and the dates. The response is `application/pdf`, an attachment, `X-Content-Type-Options: nosniff`, and `Cache-Control: private, no-store`. Another user's relocation is not found.

Exchange rates come from `open.er-api.com` (free, no key, updated once a day, latest rates only) behind `IExchangeRateProvider`. `ExchangeRateService` caches them in memory for six hours per base currency, and if a refresh fails it serves the last rates for up to seven days. Rates are only requested when something is in a currency other than the budget currency. The provider's terms ask for attribution to exchangerate-api.com wherever rates are shown, which the budget screen does not currently do. Tests replace the provider with fixed rates.

## Hosts

| | Address |
| --- | --- |
| Local API | `http://localhost:5080` |
| Local app | `http://localhost:4200` |
| Production API | `https://api.relovia.rolandschlosser.dev` on Render |
| Production database | Neon Postgres 18 |
| Production app | `https://relovia.rolandschlosser.dev` on Vercel |

Production configuration, including the Google redirect URI, is in [docs/setup.md](docs/setup.md).
