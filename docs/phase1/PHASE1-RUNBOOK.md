# Phase 1 — CMS test site runbook

**Goal:** Install Grav CMS on **`cms-test.alanstirling.com`** only.  
**Your live site (`www.alanstirling.com`) is NOT changed in Phase 1.**

Work through one step at a time. Reply in chat after each step before moving on.

---

## Phase 1 overview (all steps)

| Step | What | Where | Touches live site? |
|------|------|-------|-------------------|
| 1 | Confirm DNS for `cms-test` | Domain registrar | No |
| 2 | Run prerequisites check | VM (PowerShell) | No |
| 3 | Install PHP (if needed) | VM | No |
| 4 | Create IIS site + folder | VM (IIS) | No |
| 5 | HTTPS certificate for cms-test | VM (win-acme) | No |
| 6 | Download and install Grav | VM | No |
| 7 | Grav setup wizard | Browser | No |
| 8 | Build test home page | Grav admin | No |

---

## Step 1 — DNS record for cms-test

**What is DNS?** It tells the internet which server to use when someone types a web address.

**What we're doing:** Add a name `cms-test.alanstirling.com` that points to your VM (same server as your main site, different address).

### 1a. Log in to your domain registrar

This is where you bought `alanstirling.com` (not Azure, not the VM).

### 1b. Open DNS management

Look for: **DNS**, **DNS Records**, **Manage DNS**, or **Zone editor**.

### 1c. Add a new record

| Field | Value |
|-------|-------|
| **Type** | `A` |
| **Name / Host** | `cms-test` |
| **Value / Points to** | `20.64.215.226` |
| **TTL** | Default (e.g. 3600) is fine |

Save the record.

**Note:** Your main site uses the same IP. That is correct — one server can host many addresses.

### 1d. Verify (optional, may take up to an hour)

On your **home PC**, open PowerShell and run:

```powershell
nslookup cms-test.alanstirling.com
```

You should see `20.64.215.226`. If not, wait and try again later — DNS can take time.

### Step 1 done when:

- [ ] A record `cms-test` → `20.64.215.226` is saved at your registrar
- [ ] (Optional) nslookup shows the correct IP

**Reply in chat:** `Step 1 done`

---

## Step 2 — Prerequisites check on the VM

**What we're doing:** See what is already installed (PHP, URL Rewrite, etc.) before we add anything.

### 2a. Copy script to VM

On your **dev PC**, after pulling latest repo:

- Copy `scripts\phase1-check-prerequisites.ps1` to `C:\admin\phase0\` on the VM (same folder you used in Phase 0).

Or download directly on the VM:

```powershell
Invoke-WebRequest -Uri "https://raw.githubusercontent.com/adstirling52-droid/DnDTracker/main/scripts/phase1-check-prerequisites.ps1" -OutFile "C:\admin\phase0\phase1-check-prerequisites.ps1"
```

(Use the branch URL if not yet merged to main.)

### 2b. Run on VM (PowerShell as Administrator)

```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
cd C:\admin\phase0
.\phase1-check-prerequisites.ps1
```

### Step 2 done when:

- [ ] Script ran without errors
- [ ] You copied the output and pasted it in chat

**Reply in chat:** paste the script output + `Step 2 done`

---

## Step 3 — Install PHP (only if Step 2 says ACTION NEEDED)

*Detailed instructions will be provided after Step 2 results. Skip until then.*

---

## Step 4 — Create IIS site (only after PHP ready)

*Detailed instructions will be provided in chat when you reach this step.*

Planned values:

| Setting | Value |
|---------|-------|
| Site name | `cms-test` |
| Physical path | `C:\inetpub\cms-test` |
| Hostname binding | `cms-test.alanstirling.com` |
| App pool | `GravCMS-Test` (No Managed Code) |

---

## Step 5 — HTTPS certificate

*Use win-acme to add `cms-test.alanstirling.com` to your certificate. Instructions provided when you reach this step.*

---

## Step 6 — Install Grav

*Download Grav zip, extract to `C:\inetpub\cms-test`, set permissions. Instructions provided when you reach this step.*

---

## Step 7 — Grav setup wizard

Browse to `https://cms-test.alanstirling.com` and complete the admin account setup.

---

## Step 8 — Test home page

Recreate your current home page design in Grav as a visual test (no cutover of the live site).

---

## Rollback (Phase 1)

If anything goes wrong with the **test** site only:

1. In IIS Manager, stop or delete the `cms-test` site
2. Delete or rename `C:\inetpub\cms-test`
3. Remove the DNS record for `cms-test` (optional)

**`www.alanstirling.com` and DnDTracker are unaffected.**

---

## Glossary

| Term | Meaning |
|------|---------|
| **cms-test** | Test address only — not your public home page |
| **Grav** | The CMS we plan to use for editable pages |
| **PHP** | Programming language Grav needs (like .NET for DnDTracker) |
| **Registrar** | Company where you bought your domain name |
| **A record** | DNS entry that maps a name to an IP address |

---

*Phase 1 — do not proceed to Step 3+ until Step 2 output is reviewed.*
