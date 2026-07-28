# Phase 4 — Make Grav CMS live on www.alanstirling.com

**Goal:** `https://www.alanstirling.com` (and `https://alanstirling.com`) serve your Grav CMS. DnDTracker stays on `https://tracker.alanstirling.com` only.

**Prerequisite:** Phase 3 complete (tracker subdomain working). Grav content ready on `cms-test.alanstirling.com`.

Work through one step at a time. Reply in chat after each step before moving on.

---

## Phase 4 overview

| Step | What | Where | Downtime on www? |
|------|------|-------|------------------|
| 1 | Fresh backup | VM | No |
| 2 | Finalize Grav content + URLs | Grav admin (cms-test) | No |
| 3 | Add legacy URL redirects | VM (`C:\inetpub\cms-test`) | No |
| 4 | Move IIS bindings www → cms | VM (IIS) | **Brief** (minutes) |
| 5 | HTTPS certificate on cms site | VM (win-acme) | Maybe |
| 6 | Remove catch-all bindings from tracker site | VM (IIS) | No |
| 7 | Verify everything | Browser | No |

**What stays the same**

| Address | After Phase 4 |
|---------|----------------|
| `https://tracker.alanstirling.com/` | DnDTracker (unchanged) |
| `https://cms-test.alanstirling.com/` | Same Grav site (optional staging URL) |
| `https://www.alanstirling.com/` | **Grav CMS** (was Blazor personal page) |
| `https://www.alanstirling.com/dnd` | Redirects to tracker (via web.config) |

---

## Step 1 — Fresh backup

**What we're doing:** Snapshot the VM before we change which site answers `www`. Same idea as Phase 0.

### 1a. On the VM (PowerShell as Administrator)

```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
cd C:\admin\phase0
.\phase0-backup-vm.ps1 -BackupRoot C:\admin\backups\phase4-pre-cutover
```

### 1b. Also zip the Grav folder

```powershell
Compress-Archive -Path C:\inetpub\cms-test -DestinationPath C:\admin\backups\phase4-pre-cutover\cms-test-site.zip -CompressionLevel Optimal
```

### 1c. Copy the backup folder off the VM

Copy `C:\admin\backups\phase4-pre-cutover` to your dev PC (or cloud storage), same as Phase 0.

### Step 1 done when:

- [ ] `phase0-backup-vm.ps1` finished without errors
- [ ] `cms-test-site.zip` exists
- [ ] Backup folder copied off the VM

**Reply in chat:** `Step 1 done`

---

## Step 2 — Finalize Grav content (cms-test)

**What we're doing:** Make sure the test site content is ready to become the public home page. No IIS changes yet.

### 2a. Open Grav admin

Browse to `https://cms-test.alanstirling.com/admin`

### 2b. Check the home page

- Hero / intro text looks right
- **D&D Tracker** project link points to `https://tracker.alanstirling.com` (not `/dnd`)

### 2c. Site configuration (if you set a custom URL)

In **Configuration → System** (or edit `user/config/system.yaml` on the VM), if **Custom Base URL** is set to the cms-test hostname, change it to:

```text
https://www.alanstirling.com
```

If it is blank / automatic, you can leave it until after cutover and update in Step 7 if links look wrong.

### Step 2 done when:

- [ ] Home page content is ready for public
- [ ] D&D link goes to `https://tracker.alanstirling.com`

**Reply in chat:** `Step 2 done`

---

## Step 3 — Legacy URL redirects (web.config)

**What we're doing:** Old bookmarks to `www.alanstirling.com/dnd` (and similar) should send people to the tracker subdomain.

### 3a. On the VM, open

```text
C:\inetpub\cms-test\web.config
```

### 3b. Add redirect rules

If you already have a Grav `web.config` with rewrite rules, add the **Redirect legacy DnD paths** rules from:

`docs/phase4/cms-web.config.example` (in this repo)

Or merge manually — the important rules redirect:

| Old path | New target |
|----------|------------|
| `/dnd` | `https://tracker.alanstirling.com/` |
| `/campaigns` | `https://tracker.alanstirling.com/campaigns` |
| `/Account/*` | `https://tracker.alanstirling.com/Account/*` |

### 3c. Save the file

No IIS restart needed yet.

### Step 3 done when:

- [ ] `web.config` updated with redirect rules
- [ ] File saved

**Reply in chat:** `Step 3 done`

---

## Step 4 — Move www bindings to the cms site

**What we're doing:** IIS can only attach each hostname to **one** site. We move `www` and apex from the DnDTracker site to `cms-test`.

**Expect a short gap** on `www` between removing the old binding and adding the new one (usually under a minute).

### 4a. List current sites and bindings

```powershell
Get-Website | Format-Table Name, Id, State
Get-WebBinding | Where-Object { $_.protocol -match 'http' } | Format-Table protocol, bindingInformation, ItemXPath -AutoSize
```

Note your exact IIS site names (often `DnDtracker` and `cms-test`).

### 4b. Remove www / apex bindings from the tracker site

Replace `DnDtracker` if your site name differs:

```powershell
# Remove hostname-specific www/apex bindings (skip if not present)
Remove-WebBinding -Name "DnDtracker" -BindingInformation "*:80:www.alanstirling.com" -ErrorAction SilentlyContinue
Remove-WebBinding -Name "DnDtracker" -BindingInformation "*:443:www.alanstirling.com" -ErrorAction SilentlyContinue
Remove-WebBinding -Name "DnDtracker" -BindingInformation "*:80:alanstirling.com" -ErrorAction SilentlyContinue
Remove-WebBinding -Name "DnDtracker" -BindingInformation "*:443:alanstirling.com" -ErrorAction SilentlyContinue
```

### 4c. Add www / apex bindings to cms-test

```powershell
New-WebBinding -Name "cms-test" -Protocol http -Port 80 -HostHeader "www.alanstirling.com"
New-WebBinding -Name "cms-test" -Protocol http -Port 80 -HostHeader "alanstirling.com"
```

HTTPS bindings come in Step 5 (after certificate).

### 4d. Quick HTTP test

From your home PC:

```powershell
curl -I http://www.alanstirling.com/
```

You should get a Grav/PHP response (200 or redirect), not the Blazor personal page.

### Step 4 done when:

- [ ] www HTTP serves Grav (or redirects to HTTPS)
- [ ] `https://tracker.alanstirling.com/` still works

**Reply in chat:** `Step 4 done` (paste curl output if unsure)

---

## Step 5 — HTTPS certificate for www on cms site

**What we're doing:** Add HTTPS bindings for `www` and apex on the `cms-test` IIS site.

### 5a. win-acme

```powershell
cd C:\tools\win-acme
.\wacs.exe
```

1. **Create certificate** (full options)
2. Pick bindings from IIS — select the **`cms-test`** site
3. Include hostnames: `www.alanstirling.com` and `alanstirling.com`
4. Let win-acme create the HTTPS bindings

If win-acme asks about an existing certificate that already covers www, you may need to **re-use** or **replace** — follow the prompts for the cms site bindings.

### 5b. Verify HTTPS

```powershell
curl -I https://www.alanstirling.com/
```

### Step 5 done when:

- [ ] `https://www.alanstirling.com/` loads Grav with a valid certificate
- [ ] `https://alanstirling.com/` works (or redirects to www)

**Reply in chat:** `Step 5 done`

---

## Step 6 — Remove catch-all bindings from tracker site

**What we're doing:** The DnDTracker site may still have `*:80:` and `*:443:` bindings (no hostname). Those catch **all** traffic and can steal requests from Grav. Remove them; keep only `tracker.alanstirling.com`.

### 6a. Inspect bindings

```powershell
Get-WebBinding -Name "DnDtracker" | Format-Table protocol, bindingInformation
```

### 6b. Remove catch-all (only if present)

```powershell
Remove-WebBinding -Name "DnDtracker" -BindingInformation "*:80:" -ErrorAction SilentlyContinue
Remove-WebBinding -Name "DnDtracker" -BindingInformation "*:443:" -ErrorAction SilentlyContinue
```

**Keep** bindings that include `tracker.alanstirling.com`.

### 6c. Confirm tracker still works

Browse to `https://tracker.alanstirling.com/` and log in.

### Step 6 done when:

- [ ] Tracker site has only `tracker.alanstirling.com` bindings
- [ ] Tracker app still works

**Reply in chat:** `Step 6 done`

---

## Step 7 — Final verification

| Test | Expected |
|------|----------|
| `https://www.alanstirling.com/` | Grav home page |
| `https://alanstirling.com/` | Grav or redirect to www |
| `https://www.alanstirling.com/dnd` | Redirect to tracker |
| `https://tracker.alanstirling.com/` | DnDTracker home |
| `https://cms-test.alanstirling.com/` | Same Grav (staging) |
| Grav admin on www | `/admin` works (bookmark `https://www.alanstirling.com/admin`) |

Update Grav **Custom Base URL** to `https://www.alanstirling.com` if any links still show `cms-test`.

### Optional cleanup (later)

- Rename IIS site `cms-test` → `alanstirling-cms`
- Remove `cms-test` DNS record when you no longer need staging
- Remove `www` from DnDTracker `AllowedHosts` in `appsettings.Production.json` (cosmetic; www no longer hits the app)

---

## Rollback

If something goes wrong:

1. Remove www/apex bindings from `cms-test`
2. Re-add www/apex bindings to `DnDtracker` (and HTTPS with the previous certificate)
3. Restore `C:\inetpub\cms-test` from `cms-test-site.zip` if needed
4. Restore DnDTracker from Phase 4 backup zip if needed

Tracker subdomain is independent — it should keep working unless you changed its bindings.

---

## Troubleshooting

| Symptom | Check |
|---------|--------|
| www shows DnDTracker / Blazor page | www binding still on `DnDtracker`; remove it |
| www shows wrong site or 404 | Binding on `cms-test`? Site Started? |
| Certificate error on www | win-acme finished on **cms-test** site |
| Grav 404 on pages | URL Rewrite module installed; `web.config` present |
| `/dnd` 404 on www | Redirect rules in cms `web.config` |
| tracker broken | `tracker.alanstirling.com` bindings still on DnDtracker |

---

*Phase 4 — CMS go-live. Do not proceed past Step 1 until backup is confirmed.*
