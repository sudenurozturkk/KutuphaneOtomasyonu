# Engineering Log

## 2026-10-16 cycle, part 4 — student (member) portal

### Completed

- Core: `Uye.PortalSifreHash` / `PortalSifreGecici` (migration `UyePortalHesabi`, additive columns only).
  `UyeHesapService`: login by student number or e-mail (ambiguous student numbers rejected), PBKDF2, shared
  `GirisKilidi` lockout, staff-issued 10-character one-time password (CSPRNG, unambiguous alphabet), member password change.
  `KimlikDogrulamaService.YeniSifreHatasi` shared by staff and members. Anonymisation clears portal credentials.
- Demo data: deterministic demo student `2026000001` / `ogrenci123` with two active and two past loans (no extra RNG draws).
- API: separate member cookie scheme (`UyeOturumu`) re-validated on every request (member still allowed, password stamp
  unchanged); `/api/uye-oturum/*`, `/api/hesabim/*` (own summary, loans + renew, reservations + cancel, notifications);
  `POST /api/uyeler/{id}/portal-sifresi` (Kütüphaneci, `Cache-Control: no-store`). Temporary password forces a change.
- Web UI: Öğrenci/Personel login tabs, "Hesabım" page, "Rezerve et" on book pages (shows existing queue position),
  portal account section on the staff member page. WPF member dialog: "Portal Hesabı Aç / Şifresini Sıfırla".
- Note: part of the member-session scaffolding (`UyeOturumu.cs`, `UyeOturumEndpoints.cs`, Program/DTO wiring) was written mid-task
  by a GitHub Copilot automation the owner had left running (since removed); it was kept and completed (it did not compile: duplicate
  `SifreDegistirmeli` extension; temporary-password flag ignored; no session re-validation).

### Verification

- `KutuphaneOtomasyonu.Tests`: 144 passed. `KutuphaneOtomasyonu.Api.Tests`: 28 passed.
  Mutation checks: removing the loan-ownership check or the password-stamp check each makes its test fail.
- Build WPF + API: 0 warnings, 0 errors; `has-pending-model-changes`: none; WPF smoke: exit 0, 44 PASS, 0 FAIL.
- Headless Chrome (temp demo DB): student login, redirect when not logged in, Hesabım with loans, renew, existing-queue
  message, staff issues temporary password (confirm on reset), new student forced to change password then reserves a book,
  mobile 390 px without horizontal scroll, logout → 401; staff flow regression run passed. No script errors.

## 2026-10-16 cycle, part 3 — online system: Web API + browser UI

Course requirement: "online library management system" with a deployment diagram containing a Web API and a database.

### Completed

- `src/KutuphaneOtomasyonu.Core` (net9.0): `Models`, `Data`, `Services` moved out of the WPF project with `git mv`
  (namespaces unchanged). WPF app and tests reference Core; the test project no longer needs WPF.
- `KimlikDogrulamaService`: stateless credential check, lockout, hash upgrade and password change. `AuthService` keeps only
  the desktop session. `OduncService.IadeAlAsync/UzatAsync` take the acting user explicitly (no global session lookup).
- `src/KutuphaneOtomasyonu.Api` (ASP.NET Core 9 minimal APIs): public catalogue; cookie session (HttpOnly, SameSite=Strict,
  Secure outside Development) re-validated against the database on every request; role policies; per-IP login rate limit;
  forced password change (configurable); DTOs instead of entities; role-based masking; Problem Details; CSP and other
  security headers; OpenAPI + Swagger UI in Development; `wwwroot` dependency-free single-page UI.
- `Tests/KutuphaneOtomasyonu.Api.Tests`: 18 integration tests via `WebApplicationFactory` on a temp SQLite file.
- UML: package and deployment diagrams redrawn for the real architecture; README "Çevrimiçi Kullanım" section.

### Verification

- Build WPF + API: 0 warnings, 0 errors. `has-pending-model-changes` (Core): none.
- `KutuphaneOtomasyonu.Tests`: 140 passed. `KutuphaneOtomasyonu.Api.Tests`: 18 passed.
  Mutation check: removing authorization from the loan endpoints makes 2 tests fail.
- WPF smoke (`--test`): exit 0, 44 PASS, 0 FAIL.
- Headless Chrome on a temp demo DB: catalogue search, redirect to login, admin login, lend by copy barcode + student number,
  duplicate lend rejected with message, return by barcode, logout (session 401), no horizontal scroll at 390 px,
  light and dark themes; no script errors or CSP violations. Found and fixed: `replaceChildren(null)` rendered "null".
- Live `%APPDATA%` database timestamp unchanged.

### Notes

- With `Veritabani:Yol` empty the API uses the same database file as the desktop app. SQLite is an embedded engine, so the
  database lives on the application server; moving to a separate DB server would need a provider change (migrations
  contain SQLite-specific trigger SQL).
- Member self-service login is not implemented (members have no credentials); the public catalogue needs no login.

## 2026-10-16 cycle, part 2 — resolving all open decisions

Decisions and evidence per item: `docs/engineering-backlog.md` ("Owner decisions — resolved").

### Completed

- D-06/M-05: EF Core migrations (`Data/Migrations`, local tool `dotnet-ef` 9.0.21 in `.config/dotnet-tools.json`).
  `VeritabaniSemasi.Hazirla` baselines pre-migration databases and repairs legacy duplicate active loans (audited).
  `DatabaseSeeder` now only loads demo data.
- D-04: copy-level model `KitapNushasi` + `Odunc.NushaId`; data migration from counters; barcode desk lends/returns exact copies;
  book detail lists copies; self-heal for books without copy rows.
- D-09: `PasaportNo`; TC or passport required; filtered unique indexes.
- D-08: role-based masking, admin anonymisation (`AnonimlestirmeTarihi`), `docs/kisisel-veri-notlari.md`.
- D-05: persisted wallet ledger (`CuzdanHareketi`, `CuzdanService`, rewritten `CuzdanPage`); fictional salon occupants
  and dashboard salon numbers replaced with real reservation data.
- D-02: renewal blocked when another member waits and no free copy exists.
- D-07: Release builds force password change for default/short passwords.
- D-01: prototypes moved to `legacy/` and excluded from the build.
- M-04: `ServisKapsami` replaces duplicated service setup in pages. M-06: PBKDF2 off the UI thread; smoke harness pumps the dispatcher.
- Audit details now use invariant number formatting; barcode desk and wallet buttons have automation names.

### Verification

- Build Debug/Release: 0 warnings, 0 errors. `dotnet ef migrations has-pending-model-changes`: none.
- `dotnet test`: 137 passed, 0 failed.
- Smoke (`--test`): exit 0 on an isolated temp DB.
- Live-data upgrade (copy of `%APPDATA%` DB, through normal app start): legacy detected and baselined, 5 migrations applied,
  30 books → 79 copies (65 on shelf, 14 on loan), 14/14 active loans linked, 0 counter/copy mismatches, 50 members / 114 loans intact.
- UI Automation on that copy: lend by copy barcode (`kb0000010004`) and return by barcode only; wallet member selection and
  ₺50 top-up persisted with audit row. Live database timestamp unchanged.

### Next recommended task

- Add a FlaUI-based UI test project if UI work continues (R-01); agree retention periods with the institution (R-02).

## 2026-10-16 cycle — hardening pass (branch `engineering/hardening-2026-10`)

Backlog with evidence and IDs: `docs/engineering-backlog.md`.

### Baseline (before changes)

- `dotnet build` — 0 warnings, 0 errors.
- `dotnet test` (Tests project) — 5 passed.
- `KutuphaneOtomasyonu.exe --test` — all smoke checks passed (ran against the real `%APPDATA%` database).
- `dotnet list package --vulnerable --include-transitive` — **High**: SQLitePCLRaw.lib.e_sqlite3 2.1.10 (CVE-2025-6965).

### Progress (in order)

1. Dependencies (B-01, M-01, M-02): EF Core Sqlite/Tools 9.0.0 → 9.0.21 (SQLitePCLRaw 2.1.13), removed unused `Bogus` and `LiveChartsCore`, removed unused root `MainWindow.xaml(.cs)`. Vulnerability scan now clean.
2. Logging and smoke isolation (B-14, B-15): `UygulamaLog` writes to `%APPDATA%\KutuphaneOtomasyonu\logs\app.log`, never throws, rotates at 1 MB. `KUTUPHANE_DB_PATH` overrides the DB file; `--test` uses a temporary DB and returns a non-zero exit code on failure.
3. Authentication (B-12, B-13): PBKDF2-SHA256 (600k iterations, per-user salt); legacy SHA-256 hashes verified and upgraded on login; 5 failures → 1 minute lockout; demo credentials pre-filled only in Debug builds; logins and password changes audited; new "Şifre Değiştir" dialog.
4. Circulation (B-02…B-08): every loan/return/renew runs in one SQLite `BEGIN IMMEDIATE` transaction; DB triggers enforce `0 <= MevcutAdet <= ToplamAdet`; filtered unique index allows one active loan per member/title; archived books and expired memberships are rejected; `MaxUzatmaSayisi` comes from settings; damaged returns no longer leak inventory; returned copies are held for the next valid reservation with a fresh pick-up deadline.
   - Mutation check: with the transaction disabled, the concurrency tests fail (8 loans for 3 copies; 6 loans for a limit of 2). With it they pass.
5. Reservations (B-06): new `RezervasyonService` (create, cancel with hold hand-over, convert-to-loan atomically, expire stale).
6. Catalog/members (C-02…C-05, C-07, C-08): ISBN-10/13 and TC Kimlik checksum validation for new/changed values; friendly duplicate messages; stock edits cannot go below copies on loan; archive blocked while copies are on loan; member quotas by type; popular-books ranking fixed; RFC 4180 CSV with formula-injection guard; fine payments validated.

7. Maintenance and authorization (B-09, B-10, B-11): `VeritabaniBakimService` (online backup, transactional reset, validated settings), Admin-only at UI and service level; bulk export Admin-only with a personal-data warning and audit entry.
8. Barcode terminal (C-01): `BankoAramaService` — exact identifiers first; fuzzy title/name only when unique; multi-copy returns require the member.
9. ILL (B-16): academic requests record the publication and a chosen member; generic `SecimDialog` replaces "first book / first member".
10. Reliability (C-10, C-11): unprotected `async void` loaders now log and report errors; misleading settings field fixed.
11. Docs (M-03): README describes real patterns, data/log locations, roles, circulation rules and tests.

### Verification (final, this cycle)

- `dotnet build .\KutuphaneOtomasyonu.csproj` (Debug and Release) — 0 warnings, 0 errors.
- `dotnet test .\Tests\KutuphaneOtomasyonu.Tests\KutuphaneOtomasyonu.Tests.csproj` — 108 passed, 0 failed (was 5).
- `KutuphaneOtomasyonu.exe --test` — exit 0, all checks passed, isolated temp DB; live DB timestamp unchanged.
- `dotnet list package --vulnerable --include-transitive` — no vulnerable packages.
- Upgrade path: app started against a copy of the live `%APPDATA%` DB → reached login; triggers and index added; 30 books / 50 members / 114 loans intact.
- UI Automation (PowerShell, on the copy): admin login upgrades legacy hash and writes audit row; wrong password rejected; librarian cannot see System Settings; ILL dialog lists real books/members/campuses; password change works and old password is rejected afterwards.
- PBKDF2 cost: ~83 ms per hash on .NET 9 on this machine.

### Not verified / remaining

- No UI automation for loan/return/reservation grids; those flows are covered at service level and by the smoke round-trip.
- Owner decisions D-01…D-09 in the backlog.

### Next recommended task

- Decide D-06 (EF migrations) and D-04 (copy-level model) before further schema work; then introduce DI for services (M-04).

## 2026-10-15 cycle

### Continued cycle: isolated service test project

#### Completed

- Added constructor-based `DbContextOptions` injection while preserving the existing application database default.
- Added an isolated `net9.0-windows` xUnit project using an in-memory SQLite connection.
- Added five focused tests for salon reservation creation, active listing, duplicate rejection, different day/slot behavior, validation, and cancellation.
- Excluded the test directory from the WPF project's default source glob so test files cannot leak into the application build.
- Documented the isolated test command in `README.md`.
- Cleaned the previously reported nullable navigation and unnecessary async warnings in the academic, barcode, return-dialog, and login surfaces.

#### Changed files

- `Data/KutuphaneDbContext.cs`
- `KutuphaneOtomasyonu.csproj`
- `Tests/KutuphaneOtomasyonu.Tests/KutuphaneOtomasyonu.Tests.csproj`
- `Tests/KutuphaneOtomasyonu.Tests/SalonRezervasyonServiceTests.cs`
- `README.md`
- `Views/Pages/AkademikHubPage.xaml.cs`
- `Views/Pages/BarkodBankoPage.xaml.cs`
- `Views/Pages/IadeDialog.xaml.cs`
- `Views/LoginWindow.xaml.cs`
- `ENGINEERING_LOG.md`

#### Verification

- `dotnet test .\Tests\KutuphaneOtomasyonu.Tests\KutuphaneOtomasyonu.Tests.csproj` — 5 passed, 0 failed.
- `dotnet build .\KutuphaneOtomasyonu.csproj --no-restore` — passed with 0 warnings and 0 errors.
- `dotnet run --project .\KutuphaneOtomasyonu.csproj --no-build -- --test` — passed service smoke checks, 14 page checks, 6 dialog checks, and all navigation checks.
- `git diff --check` — passed except for existing LF/CRLF normalization notices.

#### Remaining / unverified

- UI click automation and true multi-process concurrency are still not covered.

#### Next recommended task

- Audit and fix the existing nullable warnings in academic, barcode, return-dialog, and login code, then rerun both test suites.

### Continued cycle: salon reservation service rules

#### Completed

- Extracted salon reservation persistence and business rules into `SalonRezervasyonService`.
- Centralized active-list loading, creation, duplicate prevention, and cancellation behavior.
- Added a unique filtered SQLite index for active reservations by masa, date, and time slot to protect against concurrent duplicate writes.
- Extended the smoke regression to verify create, active listing, duplicate rejection, cancellation, and rejection of a second cancellation.

#### Changed files

- `Services/SalonRezervasyonService.cs`
- `Views/Pages/SalonRezervasyonPage.xaml.cs`
- `Data/KutuphaneDbContext.cs`
- `Data/DatabaseSeeder.cs`
- `SmokeTester.cs`
- `ENGINEERING_LOG.md`

#### Verification

- `dotnet build .\KutuphaneOtomasyonu.csproj --no-restore` — passed after fixing one local-variable name collision; existing unrelated warnings remain.
- `dotnet run --project .\KutuphaneOtomasyonu.csproj --no-build -- --test` — passed the salon service regression, 14 page checks, 6 dialog checks, and all menu navigation checks.
- `git diff --check` — passed aside from existing LF/CRLF normalization notices.

#### Remaining / unverified

- No dedicated test project exists yet; service coverage currently runs through the application smoke mode.
- WPF confirmation dialogs and true multi-process concurrency remain unverified interactively.

#### Next recommended task

- Add a focused test project around `SalonRezervasyonService`, preferably with an isolated SQLite database, then cover malformed input and concurrent creation behavior.

### Continued cycle: salon reservation persistence

#### Completed

- Replaced salon reservation's in-memory-only create/cancel path with persisted `SalonRezervasyonu` records.
- Added backward-compatible SQLite table/index creation for existing installations while keeping EF Core mappings for new databases.
- Reloaded active reservations for the current day and selected time slot when the page loads, when the slot changes, and after create/cancel operations.
- Added stale-selection and concurrent-booking protection; only persisted salon reservations can be cancelled, so demo occupancy cannot be accidentally cleared.
- Added an application smoke regression covering salon reservation insert, read, status update, and cleanup.

#### Changed files

- `Models/DigerModeller.cs`
- `Data/KutuphaneDbContext.cs`
- `Data/DatabaseSeeder.cs`
- `Views/Pages/SalonRezervasyonPage.xaml.cs`
- `SmokeTester.cs`
- `ENGINEERING_LOG.md`

#### Verification

- `dotnet build .\KutuphaneOtomasyonu.csproj --no-restore` — passed with 0 errors; existing compiler warnings remain in unrelated files.
- `dotnet run --project .\KutuphaneOtomasyonu.csproj --no-build -- --test` — passed database/seed setup, salon persistence regression, 14 page construction checks, 6 dialog checks, and all menu navigation checks.
- `git diff --check` — passed; Git only reported the repository's existing LF/CRLF normalization notices.

#### Remaining / unverified

- The smoke helper verifies persistence at the EF/database level but does not drive WPF mouse clicks or two simultaneous UI processes.
- Salon occupancy for the pre-existing static `Dolu` demo entries is still illustrative; only user-created salon reservations are persisted and cancellable.

#### Next recommended task

- Add a focused service-level test project (or extract a salon reservation service) to test overlapping slots, same-day duplicate prevention, and cancellation rules without requiring WPF.

### Completed

- Audited the uncommitted reservation UI changes from the previous cycle.
- Fixed the reservation fulfillment workflow so both `Bekliyor` and `Onaylandi` reservations can be converted into loans while they remain valid.
- Added a fresh database re-check before conversion to prevent stale selections and concurrent double-processing.
- Prevented a member from creating a second active reservation for the same book.
- Prevented `KitaplarPage` filter events from running during XAML initialization, which had caused a navigation-time `NullReferenceException`.

### Changed files

- `Views/Pages/RezervasyonlarPage.xaml.cs`
- `Views/Pages/RezervasyonEkleDialog.xaml.cs`
- `Views/Pages/KitaplarPage.xaml.cs`
- `ENGINEERING_LOG.md`

### Verification

- `dotnet build .\KutuphaneOtomasyonu.csproj --no-restore` — passed with 0 warnings and 0 errors.
- Built-in application smoke test (`KutuphaneOtomasyonu.exe --test`) — passed database, 14-page, 6-dialog, and navigation checks.
- `git diff --check` — passed; only existing line-ending normalization warnings were reported by Git.

### Remaining / unverified

- The smoke helper validates construction and navigation but does not exercise every click path or persistence race interactively.
- The salon reservation page still uses in-memory demo data rather than persisted reservation records.

### Next recommended task

- Wire salon seat reservations to the existing database/service layer, or add a focused automated service test project before expanding UI features.
