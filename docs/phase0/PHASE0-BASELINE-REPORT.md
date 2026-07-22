# Phase 0 — Baseline Report

**Project:** Personal web platform migration (CMS + subdomain apps)  
**Date:** 2026-07-22  
**Status:** Phase 0 in progress — external discovery complete; VM discovery pending  
**Live site:** https://www.alanstirling.com

This document records the **current state** before any migration work. It is the rollback reference for later phases.

---

## Phase 0 checklist

| # | Task | Status | Notes |
|---|------|--------|-------|
| 1 | External discovery (DNS, TLS, live pages) | **Done** | From cloud agent, 2026-07-22 |
| 2 | Repository / code baseline | **Done** | From GitHub `main` |
| 3 | VM discovery script prepared | **Done** | `scripts/phase0-discover-vm.ps1` |
| 4 | VM backup script prepared | **Done** | `scripts/phase0-backup-vm.ps1` |
| 5 | Run discovery script on VM (RDP) | **Pending** | Requires your action |
| 6 | Run backup script on VM (RDP) | **Pending** | Requires your action |
| 7 | Export DNS from registrar | **Pending** | Requires your action |
| 8 | Screenshots of live pages | **Pending** | Requires your action |
| 9 | Phase 0 sign-off | **Pending** | After items 5–8 |

---

## 1. Current architecture (confirmed externally)

```text
Internet
   │
   ▼
alanstirling.com / www.alanstirling.com
   DNS A → 20.64.215.226
   │
   ▼
Microsoft-IIS/10.0  (HTTPS, HSTS enabled)
   │
   ▼
Single ASP.NET Core app (DnDTracker.Web)
   ├── /                 Personal landing page (Alan Stirling)
   ├── /dnd              DnDTracker home
   ├── /campaigns        Campaign list (login required)
   ├── /Account/Login    Authentication
   └── /api/*            Item images, campaign export
```

**Not yet present (expected for future phases):**

| Hostname | DNS A record | Notes |
|----------|--------------|-------|
| `tracker.alanstirling.com` | No record | Planned DnDTracker subdomain |
| `cms-test.alanstirling.com` | No record | Planned CMS test site |

---

## 2. External discovery results (2026-07-22)

### DNS

| Record | Value |
|--------|-------|
| `alanstirling.com` A | `20.64.215.226` |
| `www.alanstirling.com` A | `20.64.215.226` |
| `tracker.alanstirling.com` | No A record |
| `cms-test.alanstirling.com` | No A record |

**Requires confirmation:** Registrar DNS panel export (TTL, any CAA/MX/TXT records).

### TLS certificate (www.alanstirling.com)

| Field | Value |
|-------|-------|
| Subject | `CN = www.alanstirling.com` |
| Issuer | Let's Encrypt (`YR1`) |
| Valid from | 2026-07-21 13:33:58 GMT |
| Valid until | 2026-10-19 13:33:57 GMT |
| SANs | `alanstirling.com`, `www.alanstirling.com` |

**Implication:** New hostnames (`tracker`, `cms-test`) will need certificate renewal/extension via win-acme before HTTPS go-live.

### HTTP behaviour

| URL | Result |
|-----|--------|
| `https://www.alanstirling.com/` | 200 — personal landing page |
| `https://alanstirling.com/` | 200 — same content (apex works) |
| `https://www.alanstirling.com/dnd` | 200 — DnDTracker home |
| `https://www.alanstirling.com/Account/Login` | 200 — login page |
| `https://www.alanstirling.com/campaigns` | Redirects to `/Account/Login?ReturnUrl=%2Fcampaigns` |

### Response headers (sample)

- `server: Microsoft-IIS/10.0`
- `strict-transport-security: max-age=2592000` (HSTS ~30 days)

### Live page content (summary)

**`/`** — Title “Alan Stirling”; hero text; Projects section with D&D Tracker card linking to `/dnd`.

**`/dnd`** — Title “D&D Tracker”; login/register prompts for anonymous users.

**Requires confirmation:** Screenshots of `/`, `/dnd`, login, and logged-in campaigns view (your browser).

---

## 3. Repository baseline (GitHub `main`)

### Technology stack

| Component | Version / detail |
|-----------|------------------|
| Framework | ASP.NET Core 10 (`net10.0`) |
| UI | Blazor Server (interactive) |
| Auth | ASP.NET Core Identity |
| ORM | EF Core + SQL Server |
| Hosting model | IIS in-process |

### Key routes

| Route | Page | Auth |
|-------|------|------|
| `/` | `Home.razor` (LandingLayout) | Anonymous |
| `/dnd` | `DndHome.razor` | Anonymous |
| `/campaigns` | Campaign list | Authorize |
| `/Account/Login` | Login | Anonymous |
| `/Account/Register` | Register | Anonymous |
| `/api/items/{id}/image` | Item image API | Authorize |
| `/api/campaigns/{id}/export` | JSON export | Authorize |

### Production configuration (repo template)

From `appsettings.Production.json` / example (no secrets in repo):

- `AllowedHosts`: `www.alanstirling.com;alanstirling.com`
- Connection string: configured on server (empty in committed file)

### Deployment workflow

1. Dev PC: `.\scripts\publish-for-iis.ps1 -OutputPath .\publish`
2. Copy to VM: `C:\inetpub\DnDTracker` (documented)
3. Preserve: `appsettings.Production.json`, `Data\item-images\`
4. Recycle IIS app pool

### Documented IIS layout (from `docs/DEPLOYMENT.md`)

| Item | Documented value |
|------|------------------|
| IIS site name | `DnDTracker` |
| App pool | `DnDTracker` (No Managed Code) |
| Physical path | `C:\inetpub\DnDTracker` |
| WebSockets | Enabled (required for Blazor Server) |
| URL Rewrite | HTTP → HTTPS redirect |
| Certificate tool | win-acme (Let's Encrypt) |
| SQL | `DnDTracker` database on localhost |
| Item images | `C:\inetpub\DnDTracker\Data\item-images` |

**Requires confirmation:** Live VM matches documentation (run discovery script).

---

## 4. VM state — pending discovery

The cloud agent **cannot RDP to your Azure VM**. Run these on the VM:

### Step A — Discovery

On the VM (scripts copied from your dev PC — see `VM-RUNBOOK.md`):

```powershell
cd C:\Admin\phase0
.\phase0-discover-vm.ps1 -OutputRoot C:\admin\phase0-output
```

This exports (no secret values for connection strings):

- System RAM, CPU, OS version, disk free space
- Installed .NET, PHP, SQL Server, URL Rewrite
- IIS sites, bindings, app pools, WebSocket settings
- Certificate metadata (thumbprints, expiry, SANs)
- Redacted `web.config` / `appsettings.Production.json`
- `applicationHost.config` copy
- Item-images file count and size

### Step B — Backup

```powershell
cd C:\Admin\phase0
.\phase0-backup-vm.ps1 -BackupRoot C:\admin\backups\phase0
```

Creates:

- `DnDTracker-site.zip`
- `DnDTracker.bak` (if sqlcmd works; otherwise manual SSMS note)
- `applicationHost.config`
- win-acme folder copy (if found)

**Copy the backup folder off the VM** to your dev PC or Azure Storage before Phase 1.

### Step C — DNS registrar export

In your domain registrar, export or screenshot all records for `alanstirling.com`.

### Step D — Screenshots

Capture and store locally (not necessarily in Git):

1. `https://www.alanstirling.com/`
2. `https://www.alanstirling.com/dnd`
3. `https://www.alanstirling.com/Account/Login`
4. Logged-in campaigns list (if you have a test account)

---

## 5. Items requiring confirmation (VM / registrar)

Fill in after running discovery on the VM:

| Item | Value |
|------|-------|
| VM Azure size (e.g. B2s) | |
| Total RAM / free disk on C: | |
| .NET Hosting Bundle version | |
| PHP installed? Version? | |
| SQL Server edition | |
| Actual IIS site name(s) | |
| Actual bindings (all hostnames, ports) | |
| App pool identity | |
| win-acme install path | |
| Connection string location (JSON vs IIS env var) | |
| stdout logging enabled in web.config? | |
| Existing automated backups? | |
| Item-images file count / size | |
| Registered user count (optional) | |

---

## 6. Rollback position (Phase 0)

At end of Phase 0, you should have:

1. **Offline backup** of `C:\inetpub\DnDTracker` + SQL `.bak` + `applicationHost.config`
2. **Discovery export** folder with IIS/DNS/cert metadata
3. **This baseline report** updated with VM findings
4. **Live site unchanged** — no IIS, DNS, or code deployment changes in Phase 0

If migration phases go wrong later, restore from the Phase 0 backup and re-bind IIS as documented in the discovery export.

---

## 7. Risks identified at baseline

| Risk | Severity | Mitigation |
|------|----------|------------|
| No confirmed VM backup yet | High | Run `phase0-backup-vm.ps1` before Phase 1 |
| Certificate covers only apex + www | Medium | Plan win-acme expansion before subdomain cutover |
| Single IIS site serves CMS + app today | Info | Migration plan already separates by hostname |
| `AllowedHosts` limited to current domains | Info | Code change needed for `tracker.*` in Phase 3 |
| VM RAM unknown | Medium | Confirm in discovery; resize if < 4 GB free RAM under load |

---

## 8. Phase 0 validation

Phase 0 is **complete** when:

- [x] External discovery documented (this report, sections 1–3)
- [x] VM scripts available in repo
- [ ] Discovery script output attached (your VM run)
- [ ] Backup verified (zip opens, .bak restores to test instance or SSMS verify)
- [ ] DNS export saved
- [ ] Screenshots saved
- [ ] Section 5 table filled in
- [ ] You confirm: **“Phase 0 sign-off”** in chat

---

## 9. Next phase (after sign-off)

**Phase 1** — Install Grav on `cms-test.alanstirling.com` only. Production `www.alanstirling.com` remains unchanged.

**Requires explicit approval before Phase 1:**

- DNS A record for `cms-test.alanstirling.com`
- PHP installation on VM
- New IIS site + certificate for cms-test

---

## Appendix — Commands used for external discovery

```bash
# DNS
dig +short alanstirling.com A
dig +short www.alanstirling.com A

# TLS
openssl s_client -connect www.alanstirling.com:443 -servername www.alanstirling.com

# HTTP status
curl -sL -o /dev/null -w "%{http_code} %{url_effective}\n" https://www.alanstirling.com/
curl -sL -o /dev/null -w "%{http_code} %{url_effective}\n" https://www.alanstirling.com/dnd
curl -sL -o /dev/null -w "%{http_code} %{url_effective}\n" https://www.alanstirling.com/campaigns
```

---

*Report generated as part of Phase 0. No production changes were made.*
