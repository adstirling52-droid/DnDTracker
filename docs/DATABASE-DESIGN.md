# DnDTracker Web — Database Design (Phase 3)

This document records the planned SQL Server database for the online DnDTracker app.

## Design decisions

| Topic | Decision |
|-------|----------|
| Multi-user | Each row is scoped to a logged-in user where applicable |
| Roll tables | Per **user**, shared across all of that user's campaigns |
| Provenance | **Not included** in the web app (simpler item model) |
| Primary keys | `Guid` for app tables; ASP.NET Identity manages user ids |
| Unassigned items | Same `Item` table; `CharacterId` is null when unassigned |

## Entity relationship overview

```text
USER
 ├── CAMPAIGN
 │     ├── CHARACTER
 │     │     ├── ITEM (assigned)
 │     │     └── SKILL
 │     └── ITEM (unassigned, CharacterId is null)
 └── ROLL TABLE
       └── ROLL TABLE ROW
```

## Tables and columns

### User (ASP.NET Core Identity)

Created automatically by Identity (e.g. `AspNetUsers`). Other tables reference `UserId`.

| Column | Notes |
|--------|-------|
| Id | Identity user id |
| Email | Login |
| PasswordHash | Never store plain passwords |

### Campaign

| Column | Type | Notes |
|--------|------|-------|
| Id | Guid | Primary key |
| UserId | string | FK → User |
| Name | string | Unique per user (case-insensitive) |

### Character

| Column | Type | Notes |
|--------|------|-------|
| Id | Guid | Primary key |
| CampaignId | Guid | FK → Campaign |
| Name | string | |

### Item

| Column | Type | Notes |
|--------|------|-------|
| Id | Guid | Primary key |
| CampaignId | Guid | FK → Campaign |
| CharacterId | Guid? | FK → Character; null = unassigned |
| Name | string | |
| Description | string | |
| WhereFound | string | |
| WhenFound | string | |
| CurrentStatus | string | |
| Notes | string | |
| ImagePath | string | Server file path or URL |

### Skill

| Column | Type | Notes |
|--------|------|-------|
| Id | Guid | Primary key |
| CharacterId | Guid | FK → Character |
| Name | string | |
| Description | string | |
| Notes | string | |

### RollTable

| Column | Type | Notes |
|--------|------|-------|
| Id | Guid | Primary key |
| UserId | string | FK → User |
| Name | string | |
| Category | string | |
| TableType | string | e.g. Generic, Item, Skill |

### RollTableRow

| Column | Type | Notes |
|--------|------|-------|
| Id | Guid | Primary key |
| RollTableId | Guid | FK → RollTable |
| Number | int | |
| Name | string | |
| PhysicalDescription | string | |
| SpecialCharacteristics | string | |

## Security rule

All queries must filter by the current user:

- `Campaign` and `RollTable`: `WHERE UserId = current user`
- `Character`, `Item`: only through campaigns owned by the current user

## Desktop app comparison

| Desktop (JSON) | Web (database) |
|----------------|----------------|
| `campaigns.json` | Campaign, Character, Item, Skill tables |
| `rolltables.json` (global on PC) | RollTable per user |
| `ProvenanceEntries` on items | Not implemented on web |
| `%LocalAppData%\DnDTracker\Images` | Server uploads folder per user |

## Next phase

Phase 11: deploy to Azure VM with IIS and SQL Server — see [DEPLOYMENT.md](DEPLOYMENT.md).
