# Engineering Log

## 2026-10-15 cycle

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
