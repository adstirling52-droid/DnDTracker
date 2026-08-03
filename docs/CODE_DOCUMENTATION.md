# DnDTracker — Code Documentation

This document describes the full codebase for the **DnDTracker** repository on GitHub (`adstirling52-droid/DnDTracker`). It is intended for developers maintaining, extending, or deploying the application.

---

## Table of Contents

1. [Overview](#1-overview)
2. [Repository Structure](#2-repository-structure)
3. [Technology Stack](#3-technology-stack)
4. [Architecture](#4-architecture)
5. [Domain Model](#5-domain-model)
6. [Desktop Application (WPF)](#6-desktop-application-wpf)
7. [Web Application (Blazor Server)](#7-web-application-blazor-server)
8. [Service Layer](#8-service-layer)
9. [Authentication and Authorization](#9-authentication-and-authorization)
10. [API Endpoints](#10-api-endpoints)
11. [UI Pages and Routing](#11-ui-pages-and-routing)
12. [Data Persistence](#12-data-persistence)
13. [Import and Export](#13-import-and-export)
14. [Image Handling](#14-image-handling)
15. [Email (SendGrid)](#15-email-sendgrid)
16. [Multi-Host Routing](#16-multi-host-routing)
17. [Configuration](#17-configuration)
18. [Database Migrations](#18-database-migrations)
19. [Deployment](#19-deployment)
20. [Desktop vs Web Feature Parity](#20-desktop-vs-web-feature-parity)
21. [Development Workflow](#21-development-workflow)
22. [File Reference](#22-file-reference)

---

## 1. Overview

DnDTracker is a learning project in C# for tracking Dungeons & Dragons campaigns. It manages:

- **Campaigns** — named containers for a game session or story arc
- **Characters** — player characters within a campaign
- **Items** — equipment and loot, assignable to characters or held in an unassigned pool
- **Skills** — character abilities
- **Roll tables** — CSV-imported random tables for generating items or skills
- **Item images** — optional pictures attached to items
- **Provenance** (desktop only) — history of where/when items were found

The repository contains two applications that share the same domain concepts:

| Application | Location | Storage | Status |
|-------------|----------|---------|--------|
| Desktop (WPF) | `DnDTracker/` | Local JSON files | Feature-complete |
| Web (Blazor Server) | `DnDTracker.Web/` | SQL Server + file uploads | In active development |

The web app is deployed (or planned for deployment) at `tracker.alanstirling.com`, while `www.alanstirling.com` serves a personal landing page from the same codebase.

---

## 2. Repository Structure

```
DnDTracker/                          # WPF desktop app
├── DnDTracker.slnx
├── DnDTracker.csproj
├── App.xaml(.cs)                    # Entry point → CampaignListWindow
├── CampaignListWindow.xaml(.cs)     # Campaign list CRUD
├── CampaignWindow.xaml(.cs)         # Main campaign editor (~1340 lines)
├── RollTablesWindow.xaml(.cs)       # Roll table management
├── New*Window.xaml(.cs)             # Create/edit dialogs
├── Select*Window.xaml(.cs)          # Selection dialogs
├── ItemImageWindow.xaml(.cs)        # Image viewer
├── RollTable.cs, RollTableRow.cs    # Roll table models
├── Models/                          # Campaign, Character, Item, Skill, ProvenanceEntry
└── Services/                        # JSON persistence services

DnDTracker.Web/                      # Blazor Server web app
├── DnDTracker.Web.slnx
├── DnDTracker.Web.csproj
├── Program.cs                       # App bootstrap, DI, middleware, API routes
├── appsettings*.json
├── Data/DnDTrackerDbContext.cs      # EF Core DbContext
├── Migrations/                      # EF Core database migrations
├── Models/                          # Domain entities + settings DTOs
├── Services/                        # Business logic (8 services)
├── Components/
│   ├── App.razor, Routes.razor
│   ├── Account/                     # Auth state provider, redirect helper
│   ├── Layout/                      # Main, Landing, Empty layouts + NavMenu
│   └── Pages/                       # Razor pages
└── wwwroot/                         # Static assets (Bootstrap, CSS, JS)

docs/                                # Architecture and deployment documentation
scripts/                             # PowerShell deployment/ops scripts
```

**Approximate size:** ~8,200 lines of C#/Razor (excluding vendored Bootstrap and migration designer files).

---

## 3. Technology Stack

| Layer | Technology | Version |
|-------|------------|---------|
| Language | C# | .NET 10.0 |
| Desktop UI | WPF | `net10.0-windows` |
| Web UI | Blazor Server (interactive server components) | ASP.NET Core 10 |
| Authentication | ASP.NET Core Identity | 10.0.10 |
| ORM | Entity Framework Core | 10.0.10 |
| Database | SQL Server (LocalDB in dev, Express on VM) | — |
| Email | SendGrid | 9.29.3 |
| Frontend CSS | Bootstrap 5 (vendored) | — |
| Hosting | IIS on Azure Windows VM | — |

---

## 4. Architecture

### 4.1 Web Application Layers

```text
┌─────────────────────────────────────────────────────────┐
│  Blazor UI (Razor Components)                           │
│  Pages: Campaigns, RollTables, Account, Home            │
└──────────────────────┬──────────────────────────────────┘
                       │ injects
┌──────────────────────▼──────────────────────────────────┐
│  Service Layer                                          │
│  CampaignService, ItemService, CharacterService, etc.   │
│  (user-scoped business logic + validation)            │
└──────────────────────┬──────────────────────────────────┘
                       │ uses
┌──────────────────────▼──────────────────────────────────┐
│  Data Layer                                           │
│  DnDTrackerDbContext (EF Core) + file system images   │
└──────────────────────┬──────────────────────────────────┘
                       │
┌──────────────────────▼──────────────────────────────────┐
│  SQL Server + Data/item-images/                       │
└─────────────────────────────────────────────────────────┘
```

### 4.2 Request Pipeline (Web)

1. `UseForwardedHeaders` — trust X-Forwarded-For/Proto from IIS reverse proxy
2. `UseStatusCodePagesWithReExecute("/not-found")` — custom 404 page
3. `UseHttpsRedirection`
4. `UseAuthentication` / `UseAuthorization`
5. `UseAntiforgery`
6. `MapRazorComponents<App>()` with interactive server render mode
7. Minimal API routes for logout, image serving, and campaign export

### 4.3 Desktop Application Flow

```text
App.xaml
  └── CampaignListWindow (list/create/import/export campaigns)
        └── CampaignWindow (per-campaign editor)
              ├── New*Window dialogs (create/edit entities)
              ├── SelectCharacterWindow (item assignment)
              ├── ItemImageWindow (view images)
              └── RollTablesWindow (roll table import/roll)
```

---

## 5. Domain Model

### 5.1 Entity Relationship (Web)

```text
USER (ASP.NET Identity)
 ├── CAMPAIGN
 │     ├── CHARACTER
 │     │     ├── ITEM (assigned)
 │     │     └── SKILL
 │     └── ITEM (unassigned, CharacterId = null)
 └── ROLL TABLE
       └── ROLL TABLE ROW
```

### 5.2 Entity Descriptions

#### Campaign
| Property | Type | Description |
|----------|------|-------------|
| `Id` | `Guid` | Primary key |
| `UserId` | `string` | Owner (FK to Identity user) |
| `Name` | `string` | Unique per user (case-insensitive) |

#### Character
| Property | Type | Description |
|----------|------|-------------|
| `Id` | `Guid` | Primary key |
| `CampaignId` | `Guid` | Parent campaign |
| `Name` | `string` | Unique within campaign (case-insensitive) |

#### Item
| Property | Type | Description |
|----------|------|-------------|
| `Id` | `Guid` | Primary key |
| `CampaignId` | `Guid` | Parent campaign |
| `CharacterId` | `Guid?` | Assigned character; `null` = unassigned pool |
| `Name` | `string` | Unique within scope (character or unassigned pool) |
| `Description` | `string` | Physical description |
| `WhereFound` | `string` | Location where item was found |
| `WhenFound` | `string` | Time/era when found |
| `CurrentStatus` | `string` | e.g. "Carried by Gandalf" |
| `Notes` | `string` | Free-form notes |
| `ImagePath` | `string` | Server-relative path to uploaded image |

#### Skill
| Property | Type | Description |
|----------|------|-------------|
| `Id` | `Guid` | Primary key |
| `CharacterId` | `Guid` | Parent character |
| `Name` | `string` | Unique per character (case-insensitive) |
| `Description` | `string` | Skill description |
| `Notes` | `string` | Free-form notes |

#### RollTable
| Property | Type | Description |
|----------|------|-------------|
| `Id` | `Guid` | Primary key |
| `UserId` | `string` | Owner (shared across all user's campaigns) |
| `Name` | `string` | Unique per user (case-insensitive) |
| `Category` | `string` | Optional category label |
| `TableType` | `string` | `Generic`, `Item`, or `Skill` |

#### RollTableRow
| Property | Type | Description |
|----------|------|-------------|
| `Id` | `Guid` | Primary key |
| `RollTableId` | `Guid` | Parent table |
| `Number` | `int` | Roll number (dice result) |
| `Name` | `string` | Row name |
| `PhysicalDescription` | `string` | Description text |
| `SpecialCharacteristics` | `string` | Special properties / notes |

### 5.3 Delete Behavior (EF Core)

| Relationship | On Delete |
|--------------|-----------|
| Campaign → Characters | Cascade |
| Campaign → Items | Cascade |
| Character → Skills | Cascade |
| Character → Items | **No Action** (items must be deleted or unassigned first) |
| RollTable → Rows | Cascade |

### 5.4 Security Rule

All data access in the web app is scoped to the authenticated user:

- `Campaign` and `RollTable`: filtered by `UserId`
- `Character`, `Item`, `Skill`: accessed only through campaigns owned by the current user

---

## 6. Desktop Application (WPF)

### 6.1 Entry Point

`App.xaml` sets `StartupUri="CampaignListWindow.xaml"`.

### 6.2 Data Storage

All data is stored locally under `%LocalAppData%\DnDTracker\`:

| File/Folder | Contents |
|-------------|----------|
| `campaigns.json` | Array of all campaigns with nested characters, items, skills |
| `rolltables.json` | Global roll tables (shared across all campaigns on this PC) |
| `Images/` | Item image files (GUID-based filenames) |

### 6.3 Key Windows

| Window | File | Purpose |
|--------|------|---------|
| Campaign List | `CampaignListWindow` | List, create, edit, delete, import, export campaigns |
| Campaign Editor | `CampaignWindow` | Full campaign management UI |
| Roll Tables | `RollTablesWindow` | Import CSV tables, roll dice, create items/skills from results |
| New Campaign | `NewCampaignWindow` | Campaign name input |
| New Character | `NewCharacterWindow` | Character name input |
| New Item | `NewItemWindow` | Item field input |
| New Skill | `NewSkillWindow` | Skill field input |
| New Provenance | `NewProvenanceEntryWindow` | Provenance history entry |
| Select Character | `SelectCharacterWindow` | Character picker for item assignment |
| Select Table Type | `SelectTableTypeWindow` | Table type picker on CSV import |
| Item Image | `ItemImageWindow` | Full-size image viewer |

### 6.4 Services

#### CampaignDataService
- `LoadCampaigns()` / `SaveCampaigns()` — read/write `campaigns.json`
- `ExportCampaign()` / `ImportCampaign()` — single-campaign JSON files
- `CopyItemImageToAppFolder()` — copy image file into `Images/` folder

#### RollTableDataService
- `LoadRollTables()` / `SaveRollTables()` — read/write `rolltables.json`

### 6.5 Desktop-Only Features

- **Provenance tracking** — `ProvenanceEntry` records (What, Where, When, Notes) on items
- **Global roll tables** — not scoped to a campaign or user

---

## 7. Web Application (Blazor Server)

### 7.1 Entry Point

`Program.cs` configures and runs the ASP.NET Core host.

### 7.2 Dependency Injection Registration

| Service | Lifetime | Interface |
|---------|----------|-----------|
| `DnDTrackerDbContext` | Scoped | — |
| `SiteHostService` | Scoped | — |
| `AuthenticationStateProvider` | Scoped | `IdentityRevalidatingAuthenticationStateProvider` |
| `SendGridEmailSender` | Singleton | `IEmailSender<ApplicationUser>` |
| `CampaignService` | Scoped | — |
| `CampaignImportExportService` | Scoped | — |
| `CharacterService` | Scoped | — |
| `ItemService` | Scoped | — |
| `SkillService` | Scoped | — |
| `ItemImageService` | Scoped | — |
| `RollTableService` | Scoped | — |

### 7.3 Startup Behavior (Production)

On first run in Production:
1. EF Core migrations are applied automatically (`db.Database.Migrate()`)
2. `Data/item-images/` directory is created

### 7.4 Blazor Render Mode

Campaign and roll table pages use `@rendermode InteractiveServer` for real-time UI updates over SignalR/WebSockets.

---

## 8. Service Layer

All web services use **primary constructor injection** and accept a `userId` parameter (from `ClaimTypes.NameIdentifier`) to enforce ownership.

### 8.1 CampaignService

| Method | Description |
|--------|-------------|
| `GetCampaignsAsync(userId)` | List all campaigns for user |
| `GetCampaignAsync(userId, campaignId)` | Get single campaign |
| `NameExistsAsync(userId, name, excludeId?)` | Case-insensitive name check |
| `CreateAsync(userId, name)` | Create campaign |
| `UpdateAsync(userId, campaignId, name)` | Rename campaign |
| `DeleteAsync(userId, campaignId)` | Delete campaign (cascades characters/items) |

### 8.2 CharacterService

| Method | Description |
|--------|-------------|
| `GetByCampaignAsync(userId, campaignId)` | List characters |
| `CreateAsync(userId, campaignId, name)` | Create character |
| `UpdateAsync(userId, campaignId, characterId, name)` | Rename character |
| `DeleteAsync(userId, campaignId, characterId)` | Delete character **and all assigned items** |

### 8.3 ItemService

| Method | Description |
|--------|-------------|
| `GetUnassignedAsync(userId, campaignId)` | List unassigned items |
| `GetByCharacterAsync(userId, campaignId, characterId)` | List character items |
| `CreateUnassignedAsync(userId, campaignId, input)` | Create unassigned item |
| `CreateForCharacterAsync(userId, campaignId, characterId, input)` | Create character item |
| `UpdateAsync(userId, campaignId, itemId, input)` | Update item fields |
| `DeleteAsync(userId, campaignId, itemId)` | Delete item and its image files |
| `AssignToCharacterAsync(userId, campaignId, itemId, characterId)` | Move from unassigned to character |
| `UnassignAsync(userId, campaignId, itemId)` | Move from character to unassigned |
| `CopyAsync(userId, campaignId, sourceItemId, targetCharacterId?)` | Duplicate item (with image) |

`ItemInput` record: `Name`, `Description`, `WhereFound`, `WhenFound`, `CurrentStatus`, `Notes`.

### 8.4 SkillService

| Method | Description |
|--------|-------------|
| `GetByCharacterAsync(userId, campaignId, characterId)` | List skills |
| `CreateAsync(userId, campaignId, characterId, name, description, notes)` | Create skill |
| `UpdateAsync(...)` | Update skill |
| `DeleteAsync(...)` | Delete skill |

### 8.5 RollTableService

| Method | Description |
|--------|-------------|
| `GetAllAsync(userId)` | List user's roll tables |
| `GetWithRowsAsync(userId, rollTableId)` | Get table with ordered rows |
| `ImportFromCsvAsync(userId, tableName, tableType, csvContent)` | Parse CSV and create table |
| `DeleteAsync(userId, rollTableId)` | Delete table |
| `CreateItemInputFromRoll(row, currentStatus)` | Map roll result to `ItemInput` |
| `CreateSkillInputFromRoll(row)` | Map roll result to skill fields |

**CSV format expected:** Header row + data rows with 4 columns: `Number,Name,PhysicalDescription,SpecialCharacteristics`.

### 8.6 ItemImageService

| Constant/Method | Description |
|-----------------|-------------|
| `MaxFileSizeBytes` | 5 MB |
| Allowed extensions | `.png`, `.jpg`, `.jpeg`, `.bmp` |
| `SaveForItemAsync(userId, campaignId, itemId, stream, fileName)` | Upload and save image |
| `ClearForItemAsync(...)` | Remove image from item |
| `CopyImageForItemAsync(userId, sourceItemId, targetItemId)` | Copy image on item duplication |
| `OpenImageAsync(userId, itemId)` | Open image stream for serving |
| `GetImageUrl(itemId, version?)` | Generate cache-busted image URL |

Images stored at: `Data/item-images/{userId}/{itemId}.{ext}`

### 8.7 CampaignImportExportService

| Method | Description |
|--------|-------------|
| `ExportAsync(userId, campaignId)` | Export campaign as desktop-compatible JSON |
| `ImportAsync(userId, json)` | Import campaign from JSON (transactional) |

Import skips provenance entries and images, returning a `CampaignImportSummary` with counts.

### 8.8 SiteHostService

| Property | Description |
|----------|-------------|
| `IsTrackerHost` | `true` when hostname starts with `tracker.` |
| `AppHomePath` | `"/"` on tracker subdomain, `"/dnd"` on main domain |

---

## 9. Authentication and Authorization

### 9.1 Identity Configuration

- User model: `ApplicationUser` (no extra fields beyond Identity defaults)
- `RequireConfirmedAccount = false` — users can log in immediately after registration
- `RequireUniqueEmail = true`
- Login supports **username or email**
- Password minimum length: 6 characters (enforced in registration form)

### 9.2 Auth Pages

| Route | Access | Description |
|-------|--------|-------------|
| `/Account/Login` | Anonymous | Username/email + password login |
| `/Account/Register` | Anonymous | Create account (username, email, password) |
| `/Account/ForgotPassword` | Anonymous | Request password reset email |
| `/Account/ResetPassword` | Anonymous | Set new password via email link |
| POST `/Account/Logout` | Authenticated | Sign out |

### 9.3 Protected Pages

Pages with `[Authorize]` attribute:
- `/campaigns`
- `/campaigns/{id}`
- `/campaigns/{id}/roll-tables`

Unauthenticated users are redirected to login via `RedirectToLogin.razor`.

### 9.4 Auth State Revalidation

`IdentityRevalidatingAuthenticationStateProvider` revalidates the security stamp every 30 minutes to detect password changes or account lockouts during an active session.

### 9.5 Password Reset Flow

1. User submits email on `/Account/ForgotPassword`
2. If account exists, a reset token is generated and emailed via SendGrid
3. Email contains link to `/Account/ResetPassword?userId={id}&code={token}`
4. User sets new password; redirected to login with success message

---

## 10. API Endpoints

### 10.1 Minimal API Routes (Program.cs)

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| POST | `/Account/Logout` | Any | Sign out; redirect to home |
| GET | `/api/items/{itemId}/image` | Required | Serve item image file |
| GET | `/api/campaigns/{campaignId}/export` | Required | Download campaign JSON |
| GET | `/dev/send-test-email?to=` | Dev only | Test SendGrid configuration |

All authenticated endpoints extract `userId` from `ClaimTypes.NameIdentifier` and pass it to services for ownership checks.

---

## 11. UI Pages and Routing

### 11.1 Public Pages

| Route | Component | Layout | Description |
|-------|-----------|--------|-------------|
| `/` | `Home.razor` | Host-aware | Personal landing or tracker home |
| `/dnd` | `DndHome.razor` | Main | Tracker home (main domain only) |
| `/Account/Login` | `Login.razor` | Main | Login form |
| `/Account/Register` | `Register.razor` | Main | Registration form |
| `/Account/ForgotPassword` | `ForgotPassword.razor` | Main | Password reset request |
| `/Account/ResetPassword` | `ResetPassword.razor` | Main | Password reset form |
| `/Error` | `Error.razor` | Main | Error page |
| `/not-found` | `NotFound.razor` | Main | 404 page |

### 11.2 Authenticated Pages

| Route | Component | Description |
|-------|-----------|-------------|
| `/campaigns` | `Campaigns/Index.razor` | Campaign list with import/export |
| `/campaigns/{id}` | `Campaigns/Detail.razor` | Full campaign editor (~1480 lines) |
| `/campaigns/{id}/roll-tables` | `RollTables/Index.razor` | Roll table management |

### 11.3 Layouts

| Layout | Used For |
|--------|----------|
| `MainLayout` | Authenticated app pages (sidebar + nav) |
| `LandingLayout` | Personal home page on main domain |
| `EmptyLayout` | Host-routing wrapper on `/` |

### 11.4 Campaign Detail Page Structure

The campaign detail page (`Detail.razor`) is organized into:

- **Left column:** Character list with add/edit/remove
- **Center column:** Tabbed view (Skills / Items / Unassigned) with CRUD operations
- **Right column:** Selected item/skill detail panel with image upload
- **Modals:** Create/edit/delete dialogs for all entity types

---

## 12. Data Persistence

### 12.1 Web (SQL Server)

Connection string in `appsettings.Development.json` (LocalDB) or `appsettings.Production.json` / environment variables.

Tables created by EF Core migrations:
- App tables: `Campaigns`, `Characters`, `Items`, `Skills`, `RollTables`, `RollTableRows`
- Identity tables: `AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`, etc.

### 12.2 Desktop (JSON)

Serialization uses `System.Text.Json` with `WriteIndented = true`. No schema versioning — format is implicit from the C# model classes.

### 12.3 Image Storage

| Platform | Location |
|----------|----------|
| Desktop | `%LocalAppData%\DnDTracker\Images\{guid}.{ext}` |
| Web | `{ContentRoot}/Data/item-images/{userId}/{itemId}.{ext}` |

Web image uploads are excluded from git via `.gitignore`.

---

## 13. Import and Export

### 13.1 Desktop Format

Single-campaign JSON export matches the `DesktopCampaignDto` structure:

```json
{
  "Name": "My Campaign",
  "Characters": [
    {
      "Name": "Gandalf",
      "Items": [ { "Name": "Staff", "Description": "...", ... } ],
      "Skills": [ { "Name": "Fireball", "Description": "...", "Notes": "..." } ]
    }
  ],
  "UnassignedItems": [ ... ]
}
```

### 13.2 Web Import Behavior

- Validates campaign name uniqueness
- Validates character/skill/item name uniqueness within scope
- Runs in a database transaction (all-or-nothing)
- Skips `ProvenanceEntries` (counts them in summary)
- Skips `ImagePath` (counts them in summary)
- Returns `CampaignImportSummary` with skip counts

### 13.3 Web Export Behavior

- Exports in desktop-compatible format
- Does not include provenance or image paths
- Filename sanitized from campaign name

---

## 14. Image Handling

### Upload Flow (Web)

1. User selects image file in campaign detail UI
2. `IBrowserFile` stream passed to `ItemImageService.SaveForItemAsync`
3. Extension validated against allowlist
4. Previous image files for the item deleted
5. New file written to `Data/item-images/{userId}/{itemId}.{ext}`
6. `Item.ImagePath` updated in database
7. UI displays image via `/api/items/{itemId}/image?v={timestamp}`

### Security

- Extension allowlist (no SVG, no executables)
- 5 MB size limit (configured via `FormOptions.MultipartBodyLengthLimit`)
- Ownership verified before save, serve, or delete
- Images served only to authenticated owner

---

## 15. Email (SendGrid)

### Configuration

```json
"SendGrid": {
  "ApiKey": "...",
  "FromEmail": "noreply@tracker.alanstirling.com",
  "FromName": "DnD Tracker"
}
```

API key should be stored in `appsettings.Production.json` (not committed) or environment variables. Development can use User Secrets (`UserSecretsId` in csproj).

### SendGridEmailSender

Implements `IEmailSender<ApplicationUser>`:
- `SendConfirmationLinkAsync` — account confirmation (not currently used)
- `SendPasswordResetLinkAsync` — password reset emails
- `SendPasswordResetCodeAsync` — code-based reset (not currently used)

Throws `InvalidOperationException` if API key is missing.

---

## 16. Multi-Host Routing

The web app serves two domains from a single deployment:

| Host | Home Page | App Path | Logout Redirect |
|------|-----------|----------|-----------------|
| `www.alanstirling.com` | Personal landing (`PersonalHomeContent`) | `/dnd` | `/dnd` |
| `tracker.alanstirling.com` | Tracker home (`TrackerHomeContent`) | `/` | `/` |

Detection: `SiteHostService.IsTrackerHost` checks if `Request.Host.Host` starts with `tracker.`.

`Home.razor` at `/` uses `EmptyLayout` and switches between personal and tracker content based on hostname.

---

## 17. Configuration

### appsettings.json (base)

| Section | Keys |
|---------|------|
| `Logging` | Standard ASP.NET log levels |
| `AllowedHosts` | `*` (overridden in Production) |
| `SiteSettings` | `PublicSiteUrl`, `TrackerUrl` |
| `SendGrid` | `FromEmail`, `FromName` (no API key) |

### appsettings.Development.json

| Section | Keys |
|---------|------|
| `ConnectionStrings:DefaultConnection` | LocalDB connection string |

### appsettings.Production.json

| Section | Keys |
|---------|------|
| `ConnectionStrings:DefaultConnection` | Empty (set on VM) |
| `AllowedHosts` | `www.alanstirling.com;alanstirling.com;tracker.alanstirling.com` |

### Environment Variables (Production)

| Variable | Purpose |
|----------|---------|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ConnectionStrings__DefaultConnection` | SQL Server connection string |

### Launch Settings (Development)

- HTTP: `http://localhost:5025`
- HTTPS: `https://localhost:7055`

---

## 18. Database Migrations

| Migration | Date | Description |
|-----------|------|-------------|
| `20260716134622_InitialCreate` | 2026-07-16 | Campaign, Character, Item, Skill, RollTable, RollTableRow |
| `20260716140153_AddIdentityTables` | 2026-07-16 | ASP.NET Identity tables |

Migrations are applied automatically on Production startup. For development, run:

```bash
dotnet ef database update --project DnDTracker.Web
```

---

## 19. Deployment

See [DEPLOYMENT.md](DEPLOYMENT.md) for the full guide. Summary:

1. **Build:** `scripts/publish-for-iis.ps1` runs `dotnet publish`
2. **Target:** Azure Windows VM with IIS + SQL Server Express
3. **App path:** `C:\inetpub\DnDTracker`
4. **Preserve on deploy:** `appsettings.Production.json`, `Data/item-images/`
5. **IIS requirements:** .NET 10 Hosting Bundle, WebSockets enabled, HTTPS binding
6. **DNS:** `tracker.alanstirling.com` → VM public IP

### Migration Phases (docs/)

| Phase | Document | Purpose |
|-------|----------|---------|
| 0 | `phase0/VM-RUNBOOK.md` | Baseline VM discovery and backup |
| 1 | `phase1/PHASE1-RUNBOOK.md` | Prerequisites check |
| 3 | `phase3/PHASE3-RUNBOOK.md` | Tracker subdomain setup |
| 4 | `phase4/PHASE4-RUNBOOK.md` | Grav CMS cutover on www |
| 11 | `DEPLOYMENT.md` | Full Azure deployment |

---

## 20. Desktop vs Web Feature Parity

| Feature | Desktop | Web |
|---------|---------|-----|
| Campaign CRUD | Yes | Yes |
| Character CRUD | Yes | Yes |
| Item CRUD | Yes | Yes |
| Item assign/unassign | Yes | Yes |
| Item copy | Yes | Yes |
| Skill CRUD | Yes | Yes |
| Item images | Yes | Yes |
| Roll tables | Yes (global) | Yes (per user) |
| Roll from table → create item/skill | Yes | Yes |
| CSV import (roll tables) | Yes | Yes |
| Campaign import/export JSON | Yes | Yes |
| Provenance tracking | Yes | **No** |
| Multi-user | No | Yes |
| Authentication | N/A | ASP.NET Identity |
| Password reset email | N/A | SendGrid |

---

## 21. Development Workflow

### Prerequisites

- .NET 10 SDK
- SQL Server LocalDB (for web app development)
- Visual Studio 2022 or VS Code with C# extension
- (Desktop only) Windows with WPF support

### Running the Web App

```bash
cd DnDTracker.Web
dotnet run
```

Navigate to `http://localhost:5025` or `https://localhost:7055`.

### Running the Desktop App

Open `DnDTracker/DnDTracker.slnx` in Visual Studio and run, or:

```bash
dotnet run --project DnDTracker/DnDTracker.csproj
```

### Adding a Migration

```bash
cd DnDTracker.Web
dotnet ef migrations add MigrationName
dotnet ef database update
```

### Publishing for IIS

```powershell
.\scripts\publish-for-iis.ps1
```

---

## 22. File Reference

### Web — Core

| File | Purpose |
|------|---------|
| `Program.cs` | App entry, DI, middleware, API routes |
| `Data/DnDTrackerDbContext.cs` | EF Core context and relationships |
| `DnDTracker.Web.csproj` | Project file, NuGet packages |

### Web — Models

| File | Purpose |
|------|---------|
| `Models/ApplicationUser.cs` | Identity user |
| `Models/Campaign.cs` | Campaign entity |
| `Models/Character.cs` | Character entity |
| `Models/Item.cs` | Item entity |
| `Models/Skill.cs` | Skill entity |
| `Models/RollTable.cs` | Roll table entity |
| `Models/RollTableRow.cs` | Roll table row entity |
| `Models/SiteSettings.cs` | URL configuration |
| `Models/SendGridSettings.cs` | Email configuration |
| `Models/ImportExport/DesktopCampaignDtos.cs` | Import/export DTOs |

### Web — Services

| File | Purpose |
|------|---------|
| `Services/CampaignService.cs` | Campaign business logic |
| `Services/CharacterService.cs` | Character business logic |
| `Services/ItemService.cs` | Item business logic |
| `Services/SkillService.cs` | Skill business logic |
| `Services/RollTableService.cs` | Roll table import/CRUD |
| `Services/ItemImageService.cs` | Image upload/serve/delete |
| `Services/CampaignImportExportService.cs` | JSON import/export |
| `Services/SiteHostService.cs` | Hostname detection |
| `Services/SendGridEmailSender.cs` | Email sending |

### Web — UI

| File | Purpose |
|------|---------|
| `Components/App.razor` | HTML document shell |
| `Components/Routes.razor` | Router with auth |
| `Components/Pages/Home.razor` | Host-aware home page |
| `Components/Pages/Campaigns/Index.razor` | Campaign list |
| `Components/Pages/Campaigns/Detail.razor` | Campaign editor |
| `Components/Pages/RollTables/Index.razor` | Roll tables |
| `Components/Pages/Account/*.razor` | Auth pages |
| `Components/Layout/*.razor` | Layout components |
| `Components/Account/IdentityRevalidatingAuthenticationStateProvider.cs` | Auth revalidation |

### Desktop — Core

| File | Purpose |
|------|---------|
| `App.xaml(.cs)` | WPF entry point |
| `CampaignListWindow.xaml(.cs)` | Campaign list |
| `CampaignWindow.xaml(.cs)` | Campaign editor |
| `RollTablesWindow.xaml(.cs)` | Roll tables |
| `Services/CampaignDataService.cs` | JSON persistence |
| `Services/RollTableDataService.cs` | Roll table persistence |
| `Models/*.cs` | Domain models |

### Scripts

| File | Purpose |
|------|---------|
| `scripts/publish-for-iis.ps1` | Publish web app for IIS |
| `scripts/phase0-discover-vm.ps1` | VM baseline discovery |
| `scripts/phase0-backup-vm.ps1` | Pre-migration backup |
| `scripts/phase1-check-prerequisites.ps1` | Prerequisite validation |

---

*Last updated: July 2026. Generated from codebase review of `main` branch.*
