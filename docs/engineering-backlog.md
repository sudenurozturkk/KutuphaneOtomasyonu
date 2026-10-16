# Engineering Backlog

Status values: TODO, DONE, PARTIAL, NEEDS DECISION.
Evidence references are file paths in this repository unless noted otherwise. Test names refer to
`Tests/KutuphaneOtomasyonu.Tests`.

## P0 — Integrity, security, and broken functionality

| ID | Description | Evidence | Result | Status | Verification |
|---|---|---|---|---|---|
| B-01 | Transitive native SQLite had a High advisory (CVE-2025-6965, GHSA-2m69-gcr7-jv3q). | `dotnet list package --vulnerable`: SQLitePCLRaw.lib.e_sqlite3 2.1.10. | EF Core Sqlite/Tools 9.0.21 (SQLitePCLRaw 2.1.13). | DONE | Vulnerability scan: no vulnerable packages. |
| B-02 | Checkout was a read-check-write race (stock and loan limit). | `OduncService.OduncVerAsync` had no transaction. | Loan/return/renew/reservation ops run in one `BEGIN IMMEDIATE` transaction; DB triggers enforce `0 <= MevcutAdet <= ToplamAdet`; filtered unique index on active (member, title). | DONE | `OduncVer_EsZamanli*`, `IadeAl_EsZamanliCiftIade*`; mutation check: without the transaction 8 loans for 3 copies, 6 loans for a limit of 2. |
| B-03 | Archived books could be lent and reserved. | Dialogs listed all books; service never checked `Aktif`. | Service rejects; dialogs list active books. | DONE | `OduncVer_ArsivlenmisKitabiReddeder`, `Olustur_ArsivKitabi_*`. |
| B-04 | `Hasarli` return leaked inventory permanently. | `IadeAlAsync` Hasarli branch. | Damaged copy withdrawn (`ToplamAdet--`), invariant holds on every return path. | DONE | `IadeAl_HerDurumdaStokDegismeziKorunur`. |
| B-05 | Reservation hold not enforced; hold kept stale expiry. | `IadeAlAsync`, `OduncVerAsync`. | Held copies reserved for holder; expiry = return time + `RezervasyonGecerlilikSuresi`; expired waiters skipped. | DONE | `IadeAl_KopyayiSiradakiUyeyeAyirir_*`, `IadeAl_SuresiDolanBekleyen*`, `OduncVer_AyirmaSuresiGecince*`. |
| B-06 | Reservation → loan conversion not atomic. | `RezervasyonlarPage`. | `RezervasyonService.OdunceDonusturAsync` (single transaction). | DONE | `OdunceDonustur_*`. |
| B-07 | `MaxUzatmaSayisi` setting had no effect. | Entity default 2 always used. | Loans take the configured value. | DONE | `OduncVer_BasariliIslem*`, `Uzat_*`. |
| B-08 | Membership expiry ignored at checkout. | `OduncVerAsync`. | Expired members rejected. | DONE | `OduncVer_UyelikSuresiDolmusUyeyiReddeder`. |
| B-09 | DB backup always failed (wrong path) and raw copy would miss WAL data. | `AyarlarPage`; live DB `journal_mode = wal`. | SQLite online backup from the real DB path. | DONE | `YedekAl_WalModundakiSonVerileriDeIcerenOkunabilirKopyaUretir`. |
| B-10 | DB reset failed when an ILL transfer existed (FK without ON DELETE) and left dependants behind. | Live schema. | Transactional reset in FK-safe order; accounts, settings, audit kept. | DONE | `DemoVerisineSifirla_*`, `Veritabani_TransferKaydiOlanKitabin*`. |
| B-11 | No authorization; any role could change settings, reset/backup the DB, export all member data. | `YetkiVarMi` never called. | Admin-only in UI (menu hidden, page disabled) and in `VeritabaniBakimService`. | DONE | `YoneticiOlmayan*`, `PasifYonetici*`; UI Automation: librarian login hides System Settings. |
| B-12 | Passwords: fast SHA-256 with one global salt. | Old `DatabaseSeeder.BCryptHash`. | PBKDF2-SHA256 600k, per-user salt, constant-time compare, transparent upgrade; 5-failure lockout; password change dialog. | DONE | `Sifre_*`, `Giris_*`, `SifreDegistir_*`; UI Automation on a copy of live data: admin hash upgraded on login, password change works. ~83 ms/hash on this machine. |
| B-13 | Login pre-filled `admin/admin123` in every build. | `LoginWindow` ctor. | Debug-only; demo card hidden in Release. | DONE | Debug and Release builds. |
| B-14 | Startup logging wrote to install dir and could crash the app. | `App.xaml.cs`. | `UygulamaLog` in `%APPDATA%`, never throws, 1 MB rotation. | DONE | App start on DB copy reached login; log written. |
| B-15 | `--test` mutated the real database. | `SmokeTester`. | Temp DB, deleted afterwards; exit code reflects failures. | DONE | Live DB mtime unchanged after smoke run. |
| B-16 | Academic ILL request recorded hard-coded Book #1 and a staff user id as the member id. ILL page created transfers for "first book, first member". | `AkademikHubPage`, `KampusTransferPage`. | Publication stored in `AkademikYayinId`; real member/book/campus chosen in `SecimDialog`; service validates. | DONE | `YayinTransferi_*`, `KitapTransferi_*`; UI Automation: dialog lists real data. |

## P1 — Correctness and data quality

| ID | Description | Result | Status | Verification |
|---|---|---|---|---|
| C-01 | Barcode terminal `Contains` + `FirstOrDefault` could lend the wrong book to the wrong member and return an arbitrary member's loan. | `BankoAramaService`: exact identifiers first, fuzzy only when unique, ambiguity rejected. | DONE | `BankoAramaServiceTests`. |
| C-02 | Lowering `ToplamAdet` below copies on loan broke the invariant. | Rejected; `MevcutAdet` recomputed from active loans. | DONE | `KitapGuncelle_*`. |
| C-03 | Duplicate ISBN / e-mail / TC surfaced as raw SQLite errors. | Friendly checks (archived ISBNs included); ISBN-10/13 and TC checksums for new/changed values. | DONE | `KitapEkle_*`, `UyeEkle_*`, `Isbn_*`, `TcKimlik_*`. |
| C-04 | UI-created members always got 3 loans / 14 days. | Type-based defaults (student/guest 3/14, academic/staff 5/30, as in seed data). | DONE | `UyeEkle_TipeGoreKotaAtanir`. |
| C-05 | Popular-books ranking unordered. | Ordered by loan count. | DONE | `PopulerKitaplar_*`. |
| C-06 | Dashboard pending fines read the wrong source (₺0 while 9 members were blocked). | Uses member balances (the eligibility source). | DONE | `CezaOde_*` (statistic). |
| C-07 | CSV export lacked escaping/formula guard. | `CsvYazici`. | DONE | `Csv_*`. |
| C-08 | `CezaOdeAsync` accepted invalid amounts and settled records out of order. | Validated, oldest-first. | DONE | `CezaOde_*`. |
| C-09 | `AktiviteLog` never written. | `DenetimService`, committed with the operation; IDs only, no PII. | DONE | Assertions across suites. |
| C-10 | Several `async void` loaders had no exception handling (any DB error crashed the app). | Logged + user message. | DONE | Build + smoke. |
| C-11 | "Uzatma Süresi" settings field showed a hard-coded 7 that was never saved. | Shows the real value read-only. | DONE | Code review. |

## P2 — Maintainability and hygiene

| ID | Description | Status | Verification |
|---|---|---|---|
| M-01 | Removed unused `Bogus`, `LiveChartsCore.SkiaSharpView.WPF`. | DONE | Build. |
| M-02 | Removed unused root `MainWindow.xaml(.cs)`. | DONE | Build. |
| M-03 | README described patterns that do not exist in the WPF app. | DONE | Review. |
| M-04 | Views duplicated context + settings + service construction. `ServisKapsami` (unit-of-work scope) now owns settings lookup and lazy service creation; all duplicated setups replaced. A full DI container was not introduced: pages are created with `new` by `MainWindow`, so a container would add complexity without a testability gain over the scope. | DONE | `ServisKapsami_*`; smoke. |
| M-05 | Seeder mixed schema and demo data. Schema now lives in `VeritabaniSemasi` (migrations); `DatabaseSeeder` only loads demo data. | DONE | Tests, smoke. |
| M-06 | PBKDF2 ran on the UI thread. Hashing now runs via `Task.Run`; the smoke harness pumps the dispatcher instead of blocking it. | DONE | Smoke (would deadlock otherwise), UI login runs. |

## Owner decisions — resolved (decision taken, implemented, verified)

| ID | Decision | Rationale | Verification |
|---|---|---|---|
| D-01 | Legacy WinForms and web prototypes moved to `legacy/` (history kept via `git mv`), excluded from the build, documented in `legacy/README.md`. | Preserves coursework artifacts without confusing the product code. | Build. |
| D-02 | Renewal refused when another member is waiting (`Bekliyor`, valid) **and** no unheld copy is free. | Standard ILS rule; still allows renewal when the waiting member can be served from another copy. | `Uzat_BaskaUyeBekliyor*`. |
| D-03 | Damaged returns stay withdrawn from circulation (copy status `Hasarli`); staff re-add a repaired copy by raising the total. | Avoids silently lending a damaged copy; now visible per copy. | `IadeAl_*` copy-status tests. |
| D-04 | Copy-level model `KitapNushasi` (unique barcode, status, shelf); loans reference the copy; title counters are kept as a cache maintained in the same transaction; holds remain title-level. Migration builds copies from existing counters, links active loans, flags unaccounted copies as out of circulation. | Real barcode circulation and exact returns without a breaking UI rewrite. | `NushaVeUzatmaTests`, `Hazirla_EskiSayaclardanNushaUretir*`; live-data copy: 79 copies, 14/14 loans linked, 0 mismatches; UI: lend/return by copy barcode. |
| D-05 | Wallet persisted as an append-only ledger (`CuzdanHareketi`); balance = sum of movements; fine payment through `UyeService.CezaOdeAsync` in one transaction. Fictional salon occupants and dashboard salon figures removed; occupancy comes only from stored reservations. | No fake data presented as real. | `Cuzdan_*`, `Salon_GunlukOzet*`; UI: top-up persisted with audit. |
| D-06 | EF Core migrations with automatic baselining of pre-migration databases (missing baseline objects created `IF NOT EXISTS`, history row inserted) plus an audited repair of duplicate active loans. | Safe evolution for existing installs; enables table rebuilds (e.g. FK added in the copy migration). | `SemaYukseltmeTests` (5 cases); live-data copy upgraded through app start; `has-pending-model-changes`: none. |
| D-07 | Release builds force a password change after login with a known default or < 8-character password; demo passwords rejected as new passwords. Debug (demo) builds skip the prompt. | Default credentials cannot reach production use; demo flow keeps working. | `SifreDegistirilmeli_*`, `SifreDegistir_*`; Release build. |
| D-08 | Role-based masking (assistant staff see last 4 chars of ID/passport/phone, cannot edit identity data); admin anonymization for erasure requests; privacy note `docs/kisisel-veri-notlari.md`. Retention periods and legal texts remain the institution's decision (listed in the note). | Data minimisation and right to erasure without destroying statistics/financial records. | `Anonimlestir_*`, `KisiselVeri_*`. |
| D-09 | `PasaportNo` added; TC/YKN **or** passport required; both unique only when filled (filtered indexes). | International members can register. | `Uye_PasaportIleKayitOlur*`. |

## Remaining (not blocking)

| ID | Description |
|---|---|
| R-01 | No UI automation suite in the repo; UI checks were run with ad-hoc UI Automation scripts during this cycle. Consider FlaUI-based tests if the UI keeps growing. |
| R-02 | Retention periods / automatic anonymisation schedule need an institutional policy (see privacy note). |
| R-03 | Wallet top-ups are staff-recorded desk payments; online payment integration is out of scope. |
