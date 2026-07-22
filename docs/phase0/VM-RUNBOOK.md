# Phase 0 — VM runbook

Run these steps on the **Azure VM** via Remote Desktop. Phase 0 does **not** change the live website.

Your normal workflow is **git on your dev PC, copy files to the VM** (same as publishing DnDTracker). You do **not** need Git installed on the VM.

## Prerequisites

- RDP access to the VM
- Administrator PowerShell on the VM
- Dev PC with the repo pulled (`git pull origin main` after PR #34 is merged)

## 1. Get the scripts onto the VM

**Recommended — copy from your dev PC (matches how you deploy today):**

On your **dev PC**, after pulling the latest repo:

1. Locate these two files in your local clone:
   - `scripts\phase0-discover-vm.ps1`
   - `scripts\phase0-backup-vm.ps1`
2. Copy them to the VM via RDP (clipboard, shared drive, or zip).
3. On the VM, put them in a simple folder, for example:

```text
C:\Admin\phase0\
```

You do **not** need the full repo on the VM — only these two scripts.

**Alternative — zip from dev PC:**

```powershell
# On dev PC, in repo root
Compress-Archive -Path scripts\phase0-discover-vm.ps1, scripts\phase0-backup-vm.ps1 -DestinationPath phase0-scripts.zip
```

Copy `phase0-scripts.zip` to the VM, extract to `C:\Admin\phase0\`.

**Optional — Git on VM:** Only if you later choose to clone the repo on the server. Not required for Phase 0.

## 2. Run discovery

On the VM, in **elevated PowerShell**:

```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
cd C:\Admin\phase0
.\phase0-discover-vm.ps1 -OutputRoot C:\admin\phase0-output
```

Review the output folder. Check:

- `iis-sites.csv` and `iis-bindings.csv` match what you expect
- `certificates.csv` shows Let's Encrypt cert and SANs
- `disks.csv` shows sufficient free space (recommend > 10 GB free for migration work)

## 3. Run backup

Still in `C:\Admin\phase0`:

```powershell
.\phase0-backup-vm.ps1 -BackupRoot C:\admin\backups\phase0
```

If `sqlcmd` is not installed, open **SSMS** and back up the `DnDTracker` database manually to the same backup folder (see `sql-backup-NOTE.txt` if created).

**Verify the backup:**

- Open `DnDTracker-site.zip` — confirm files are present
- In SSMS, verify the `.bak` file (optional: restore to a test database name)

## 4. Copy backups off the VM

Copy the entire timestamped folder from `C:\admin\backups\phase0\<timestamp>` to:

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
