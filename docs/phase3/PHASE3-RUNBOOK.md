# Phase 3 — Move DnDTracker to tracker.alanstirling.com

**Goal:** DnDTracker works at `https://tracker.alanstirling.com` while `www.alanstirling.com` keeps your personal page until a later CMS cutover.

**Prerequisite:** Fresh backup from Phase 0 (or run `phase0-backup-vm.ps1` again before starting).

---

## What changes

| Address | Before Phase 3 | After Phase 3 |
|---------|----------------|---------------|
| `www.alanstirling.com/` | Personal landing page | Same (unchanged for now) |
| `www.alanstirling.com/dnd` | DnDTracker home | Same (still works) |
| `tracker.alanstirling.com/` | (nothing) | **DnDTracker home** |
| `tracker.alanstirling.com/dnd` | (nothing) | Redirects to `/` |

Both hostnames use the **same IIS site and folder** until you split them in a later phase.

---

## Part A — Merge code and publish (dev PC)

1. Merge the GitHub PR for Phase 3 (branch `cursor/tracker-subdomain-9c2b`).
2. On your dev PC:

```powershell
cd path\to\DnDTracker
git pull origin main
.\scripts\publish-for-iis.ps1 -OutputPath .\publish
```

3. Copy the `publish` folder to the VM over `C:\inetpub\DnDTracker` **except**:
   - Keep `appsettings.Production.json` (see Part B)
   - Keep `Data\item-images\`

---

## Part B — Update production config on the VM

Edit `C:\inetpub\DnDTracker\appsettings.Production.json` on the server.

Add `tracker.alanstirling.com` to **AllowedHosts** and add **SiteSettings**:

```json
"AllowedHosts": "www.alanstirling.com;alanstirling.com;tracker.alanstirling.com",
"SiteSettings": {
  "PublicSiteUrl": "https://www.alanstirling.com",
  "TrackerUrl": "https://tracker.alanstirling.com"
}
```

Keep your existing **ConnectionStrings** block unchanged.

If you use IIS environment variables for the connection string instead of JSON, you only need to update AllowedHosts/SiteSettings in JSON (or add SiteSettings there while connection string stays in IIS).

---

## Part C — DNS record

At your domain registrar, add:

| Type | Name | Value |
|------|------|-------|
| A | `tracker` | `20.64.215.226` |

Verify on your home PC:

```powershell
nslookup tracker.alanstirling.com
```

Should show `20.64.215.226`.

---

## Part D — Add IIS binding for tracker

**PowerShell as Administrator on the VM:**

```powershell
& $env:windir\system32\inetsrv\appcmd.exe set site "DnDTracker" /+bindings.[protocol='http',bindingInformation='*:80:tracker.alanstirling.com']
```

If your IIS site has a different name than `DnDTracker`, replace it. Check with:

```powershell
& $env:windir\system32\inetsrv\appcmd.exe list sites
```

**Do not remove** the `www.alanstirling.com` bindings yet.

---

## Part E — HTTPS certificate for tracker

1. **PowerShell as Administrator:**

```powershell
cd C:\tools\win-acme
.\wacs.exe
```

2. Create certificate (full options).
3. Pick bindings from IIS — select the **DnDTracker** site.
4. When asked which hostnames, include **`tracker.alanstirling.com`** (you can pick all bindings on that site or type the hostname).
5. Let win-acme add the HTTPS binding.

---

## Part F — Recycle the app pool

```powershell
Restart-WebAppPool -Name "DnDTracker"
```

(Use your actual app pool name if different.)

---

## Part G — Verify

| Test | Expected |
|------|----------|
| `https://tracker.alanstirling.com/` | DnDTracker home (login / welcome) |
| `https://tracker.alanstirling.com/dnd` | Redirects to `/` |
| `https://tracker.alanstirling.com/campaigns` | Login required |
| Log in on tracker | Lands on `/` or campaigns |
| Nav **Home** on tracker | Goes to `https://www.alanstirling.com` |
| `https://www.alanstirling.com/` | Personal page (unchanged) |
| `https://www.alanstirling.com/dnd` | DnDTracker still works |
| Personal page D&D link | Goes to `https://tracker.alanstirling.com` |

---

## Part H — Update cms-test Grav link (optional)

In Grav admin on `cms-test.alanstirling.com`, edit your home page D&D Tracker link to:

```text
https://tracker.alanstirling.com
```

---

## Rollback

1. Remove tracker HTTP/HTTPS bindings in IIS (leave www bindings).
2. Redeploy previous publish folder from backup if needed.
3. Remove DNS `tracker` A record (optional).

`www.alanstirling.com` continues working throughout rollback.

---

## Troubleshooting

| Symptom | Check |
|---------|--------|
| Bad host / refused | `AllowedHosts` includes `tracker.alanstirling.com` |
| 404 on tracker | Bindings point to `C:\inetpub\DnDTracker`; site Started |
| Certificate error | win-acme finished; HTTPS binding on tracker hostname |
| Login cookies odd | Log in again on tracker (cookies are per-hostname) |
| Blazor disconnect | WebSockets enabled on the site |

---

*Phase 3 — `www` personal site stays until a later CMS cutover phase.*
