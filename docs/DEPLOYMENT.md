# DnDTracker Web — Deployment Guide (Phase 11)

This guide covers hosting the web app on an **Azure Windows VM** with **IIS**, **SQL Server**, and the domain **www.alanstirling.com**.

Work through it in order. You can pause after any section and resume later.

---

## Architecture

```text
Internet
   │
   ▼
www.alanstirling.com  (DNS → Azure VM public IP)
   │
   ▼
IIS (HTTPS, WebSockets enabled)
   │
   ▼
ASP.NET Core 10 app (DnDTracker.Web)
   │
   ├── SQL Server (DnDTracker database)
   └── C:\inetpub\DnDTracker\Data\item-images\  (uploaded item images)
```

---

## Part 1 — Azure VM

### 1.1 Create the virtual machine

1. In the [Azure Portal](https://portal.azure.com), create a **Virtual machine**.
2. Recommended starting point:
   - **OS:** Windows Server 2022 Datacenter
   - **Size:** `B2s` or larger (2 vCPU, 4 GB RAM minimum for IIS + SQL Server)
   - **Public inbound ports:** RDP (3389), HTTP (80), HTTPS (443)
3. Set an admin username and password (store these safely).
4. Create the VM and note its **public IP address**.

### 1.2 Connect and update Windows

1. Connect with **Remote Desktop** using the VM public IP.
2. Open **Windows Update** and install all updates.
3. Reboot if prompted.

---

## Part 2 — Install server software

Run these on the VM.

### 2.1 .NET 10 Hosting Bundle

The Hosting Bundle installs the ASP.NET Core runtime and the **IIS module** required to run the app.

1. Download the latest **.NET 10 Hosting Bundle** from:
   https://dotnet.microsoft.com/download/dotnet/10.0
2. Run the installer.
3. Restart IIS (or reboot the VM):

```powershell
net stop was /y
net start w3svc
```

### 2.2 SQL Server

For a single VM, **SQL Server Express** is enough to start.

1. Download SQL Server Express from Microsoft.
2. Install with **Database Engine Services**.
3. Use **Mixed Mode** authentication and set a strong `sa` password.
4. After install, open **SQL Server Management Studio (SSMS)** and connect to the instance.

### 2.3 Create the database and login

Run in SSMS (adjust the password):

```sql
CREATE DATABASE DnDTracker;
GO

CREATE LOGIN DnDTrackerApp WITH PASSWORD = 'ReplaceWithAStrongPassword!';
GO

USE DnDTracker;
CREATE USER DnDTrackerApp FOR LOGIN DnDTrackerApp;
ALTER ROLE db_owner ADD MEMBER DnDTrackerApp;
GO
```

Keep the connection string details — you will need them on the server.

Example connection string:

```text
Server=localhost;Database=DnDTracker;User Id=DnDTrackerApp;Password=ReplaceWithAStrongPassword!;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=true
```

### 2.4 IIS features

Open **Server Manager → Add Roles and Features** and ensure these are installed:

- Web Server (IIS)
- WebSockets (under Application Development)
- ASP.NET 4.8 (optional but harmless; the app uses ASP.NET Core)

Or run in an elevated PowerShell session:

```powershell
Install-WindowsFeature Web-Server, Web-WebSockets
```

---

## Part 3 — Publish the app from your dev PC

On your development machine (where you build the project):

```powershell
cd path\to\DnDTracker
.\scripts\publish-for-iis.ps1 -OutputPath .\publish
```

This creates a `publish` folder with everything IIS needs.

Copy that entire folder to the VM, for example:

```text
C:\inetpub\DnDTracker
```

You can use Remote Desktop copy/paste, a zip file, or an SFTP tool.

---

## Part 4 — Configure the app on the server

### 4.1 Production settings

On the VM, in `C:\inetpub\DnDTracker`:

1. Copy `appsettings.Production.json.example` to `appsettings.Production.json` if the file is not already there.
2. Set the real `DefaultConnection` value.

**Do not commit production passwords to Git.** Keep secrets only on the server.

Alternative: set the connection string as an IIS environment variable (see section 5.2).

### 4.2 Data folders

The app stores uploaded images under:

```text
C:\inetpub\DnDTracker\Data\item-images
C:\inetpub\DnDTracker\Data\npc-images
```

Create the folders if they do not exist. The app also creates them on startup in Production.

### 4.3 Folder permissions

The IIS application pool identity needs **Modify** permission on:

- `C:\inetpub\DnDTracker\Data`
- `C:\inetpub\DnDTracker\Data\item-images`
- `C:\inetpub\DnDTracker\Data\npc-images`
- `C:\inetpub\DnDTracker\logs` (stdout startup logs)

In Explorer: right-click the folder → **Properties → Security → Edit** → add `IIS AppPool\DnDTracker` (after you create the app pool in the next section) with Modify rights.

---

## Part 5 — IIS site setup

### 5.1 Application pool

1. Open **IIS Manager**.
2. **Application Pools → Add Application Pool**
   - Name: `DnDTracker`
   - .NET CLR version: **No Managed Code**
   - Start application pool immediately: checked
3. Select the pool → **Advanced Settings**
   - **Identity:** `ApplicationPoolIdentity` (default)

### 5.2 Environment (recommended)

With the app pool selected, open **Configuration Editor**:

- Section: `system.applicationHost/applicationPools`
- Path: `DnDTracker`
- Expand **environmentVariables** and add:

| Name | Value |
|------|-------|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ConnectionStrings__DefaultConnection` | your full SQL connection string |

Using an environment variable avoids editing JSON on the server for future password changes.

### 5.3 Website

1. **Sites → Add Website**
   - Site name: `DnDTracker`
   - Application pool: `DnDTracker`
   - Physical path: `C:\inetpub\DnDTracker`
   - Binding (initial): HTTP, port 80, host name blank or `www.alanstirling.com`
2. Select the site → **Configuration Editor → system.webServer/aspNetCore**
   - Confirm `hostingModel` is `inprocess` (default after Hosting Bundle install)

### 5.4 WebSockets (required for Blazor Server)

1. Select the site in IIS Manager.
2. Open **Configuration Editor**.
3. Section: `system.webServer/webSocket`
4. Set `enabled` to **True**.
5. Apply.

### 5.5 HTTPS certificate

**Option A — Let's Encrypt (free, recommended for production)**

Use a tool such as **win-acme** on the VM to obtain a certificate for `www.alanstirling.com` and `alanstirling.com`, and let it configure the IIS HTTPS binding.

**Option B — Temporary self-signed (testing only)**

Create a self-signed cert in IIS for initial testing. Browsers will show a security warning.

Add an HTTPS binding on port **443** with the certificate selected.

### 5.6 HTTP to HTTPS redirect

Install the **URL Rewrite** module for IIS, then add a site-level rule:

- If `{HTTPS}` is `off`
- Redirect to `https://{HTTP_HOST}{REQUEST_URI}`

---

## Part 6 — DNS

At your domain registrar (where `alanstirling.com` is managed):

| Type | Name | Value |
|------|------|-------|
| A | `@` | Azure VM public IP |
| A | `www` | Azure VM public IP |

DNS changes can take up to an hour (sometimes longer) to propagate.

---

## Part 7 — First run and database migration

When the site starts in **Production**, the app automatically runs EF Core migrations against the configured database.

1. Start the site in IIS.
2. Browse to `https://www.alanstirling.com`.
3. If you see an error, check:
   - **Event Viewer → Windows Logs → Application**
   - `C:\inetpub\DnDTracker\logs\` (if stdout logging is enabled in `web.config`)

4. Register a user account and confirm login, campaigns, and image upload work.

---

## Part 8 — Deploying updates

When you change the app:

1. On your dev PC:

```powershell
.\scripts\publish-for-iis.ps1 -OutputPath .\publish
```

2. On the VM, stop the site in IIS (optional but safer).
3. Copy the new publish output over `C:\inetpub\DnDTracker` **except**:
   - Keep `appsettings.Production.json` (your secrets)
   - Keep `Data\item-images\` (user uploads)
   - Keep `Data\npc-images\` (saved NPC portraits)
4. Recycle the IIS application pool (or restart the site).
5. Start the site again. Migrations run automatically on startup.

If the site fails with **HTTP 500.30** after an update:

1. Check these startup log locations (first one the app pool can write wins):
   - `C:\inetpub\DnDTracker\startup.log`
   - `C:\inetpub\DnDTracker\Data\startup.log`
   - `%TEMP%\DnDTracker-startup.log`
2. If no log file exists, IIS failed before the app started. Run this in an elevated PowerShell session on the VM to see the error directly:

```powershell
cd C:\inetpub\DnDTracker
$env:ASPNETCORE_ENVIRONMENT = "Production"
$env:ConnectionStrings__DefaultConnection = "<paste your IIS app pool connection string here>"
dotnet .\DnDTracker.Web.dll
```

3. Also check **Event Viewer → Windows Logs → Application** for `.NET Runtime` or `IIS AspNetCore Module V2` errors.
4. Confirm the deployed DLL contains migration `20260803143258_AddCampaignNpcs`:

```powershell
findstr 20260803143258 C:\inetpub\DnDTracker\DnDTracker.Web.dll
```

5. Ensure the app pool identity has **Modify** on `C:\inetpub\DnDTracker\Data` (needed to create `Data\npc-images` on first startup after PR `#56`).
6. Recycle the IIS application pool after copying files.

---

## Troubleshooting

| Symptom | Things to check |
|---------|-----------------|
| HTTP 500.30 / app won't start | Hosting Bundle installed? `ASPNETCORE_ENVIRONMENT=Production` set? Read `Data\startup.log` and `logs\stdout*.log`. Recycle the app pool after copying files. |
| Database error on startup | Connection string correct? SQL Server running? Login has `db_owner`? See `Data\startup.log` for applied/pending migration lists. |
| 500.30 after PR `#56`/`#57` deploy | Ensure build includes migration `20260803143258_AddCampaignNpcs` (PR `#57`), not the earlier unregistered hand-written migration from PR `#56`. |
| Blazor disconnects immediately | WebSockets enabled on the IIS site? |
| HTTPS redirect loop | Forwarded headers / URL Rewrite rule correct? |
| Image upload fails | `Data\item-images` folder exists and app pool has Modify permission on `Data` |
| NPC portrait upload fails | `Data\npc-images` folder exists and app pool has Modify permission on `Data` |
| 403 / forbidden | App pool identity can read `C:\inetpub\DnDTracker` |

---

## Security checklist (before going live)

- [ ] Strong SQL password; `sa` login disabled or rarely used
- [ ] HTTPS only (HTTP redirects to HTTPS)
- [ ] `AllowedHosts` set to your real domain
- [ ] Windows Firewall allows only 80, 443, and RDP (restrict RDP to your IP if possible)
- [ ] Regular Windows Updates enabled
- [ ] Production secrets only on the server, never in Git

---

## Next steps after deployment

- Restrict who can register (admin approval, invite-only, or disable public registration)
- Set up automated backups for the SQL database and `Data\item-images`
- Optional: Azure Backup for the VM
