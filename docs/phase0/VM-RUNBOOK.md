# Phase 0 — VM runbook

Run these steps on the **Azure VM** via Remote Desktop. Phase 0 does **not** change the live website.

## Prerequisites

- RDP access to the VM
- Administrator PowerShell
- Latest `main` branch from GitHub (or copy the two scripts below to the VM)

## 1. Get the scripts onto the VM

**Option A — Git (if repo is on VM):**

```powershell
cd C:\path\to\DnDTracker
git pull origin main
```

**Option B — Copy manually:**

Copy these files from the repo to the VM:

- `scripts/phase0-discover-vm.ps1`
- `scripts/phase0-backup-vm.ps1`

## 2. Run discovery

```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
cd C:\path\to\DnDTracker
.\scripts\phase0-discover-vm.ps1 -OutputRoot D:\phase0-output
```

Review the output folder. Check:

- `iis-sites.csv` and `iis-bindings.csv` match what you expect
- `certificates.csv` shows Let's Encrypt cert and SANs
- `disks.csv` shows sufficient free space (recommend > 10 GB free for migration work)

## 3. Run backup

```powershell
.\scripts\phase0-backup-vm.ps1 -BackupRoot D:\Backups\phase0
```

If `sqlcmd` is not installed, open **SSMS** and back up the `DnDTracker` database manually to the same backup folder (see `sql-backup-NOTE.txt` if created).

**Verify the backup:**

- Open `DnDTracker-site.zip` — confirm files are present
- In SSMS, verify the `.bak` file (optional: restore to a test database name)

## 4. Copy backups off the VM

Copy the entire timestamped folder from `D:\Backups\phase0\<timestamp>` to:

- Your dev PC, or
- Azure Storage / external drive

Do not rely on the VM as the only copy.

## 5. Export DNS from registrar

Log in to your domain registrar and export or screenshot all DNS records for `alanstirling.com`. Store with your Phase 0 files.

## 6. Screenshots

Save browser screenshots of:

1. https://www.alanstirling.com/
2. https://www.alanstirling.com/dnd
3. https://www.alanstirling.com/Account/Login
4. Campaigns page (logged in), if available

## 7. Sign off

Update `docs/phase0/PHASE0-BASELINE-REPORT.md` section 5 with VM discovery values, or send the discovery output folder for review.

Reply with **“Phase 0 sign-off”** when complete to proceed to Phase 1 planning/implementation.

## Troubleshooting

| Problem | Action |
|---------|--------|
| `Import-Module WebAdministration` fails | Install IIS Management Scripts feature |
| Site path not `C:\inetpub\DnDTracker` | Pass `-SitePath` to backup script; note actual path in baseline report |
| SQL backup permission denied | Use Windows auth in SSMS as admin; or specify SQL login |
| Script execution blocked | Use `Set-ExecutionPolicy -Scope Process Bypass` for the session only |
