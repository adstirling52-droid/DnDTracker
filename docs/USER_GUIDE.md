# DnDTracker User Guide

DnDTracker is a web application for **Dungeon Masters** to manage tabletop RPG campaigns during play. Use it to track campaigns, player characters, items, skills, roll tables, and NPCs — all from your browser, with your data kept private to your account.

**Live app:** [https://tracker.alanstirling.com](https://tracker.alanstirling.com)

---

## Table of contents

1. [What DnDTracker does](#what-dndtracker-does)
2. [Getting started](#getting-started)
3. [Navigating the app](#navigating-the-app)
4. [Campaigns](#campaigns)
5. [Characters, skills, and items](#characters-skills-and-items)
6. [Rollable tables](#rollable-tables)
7. [NPC Generator and saved NPCs](#npc-generator-and-saved-npcs)
8. [Import and export](#import-and-export)
9. [Account management](#account-management)
10. [Tips for running sessions](#tips-for-running-sessions)
11. [Troubleshooting](#troubleshooting)
12. [Appendix: CSV format for roll tables](#appendix-csv-format-for-roll-tables)

---

## What DnDTracker does

DnDTracker helps you keep campaign information organized at the table:

| Feature | Description |
|---------|-------------|
| **Campaigns** | Create and manage separate campaigns, each with its own characters and loot |
| **Characters** | Track player characters within a campaign |
| **Items** | Record treasure, artifacts, and gear — assigned to a character or held in an unassigned pool |
| **Skills** | Track custom skills, proficiencies, or abilities per character |
| **Rollable tables** | Import CSV random tables and roll results directly into your campaign |
| **NPC Generator** | Generate random NPCs with appearance, personality, hooks, and DM notes |
| **Saved NPCs** | Store generated NPCs in a campaign, mark active NPCs, and upload portraits |

### What DnDTracker does not do

DnDTracker is focused on campaign inventory and NPC management. It does **not** include:

- Combat or initiative tracking
- Full character sheets, ability scores, or dice rolling (except roll-table picks)
- Shared access between multiple user accounts (each account has its own data)

---

## Getting started

### Requirements

- A modern web browser with **WebSocket** support (Chrome, Firefox, Safari, Edge, and similar)
- An internet connection (the app updates in real time over a live connection)

No installation is required to use the hosted app at [tracker.alanstirling.com](https://tracker.alanstirling.com).

### Create an account

1. Open [https://tracker.alanstirling.com](https://tracker.alanstirling.com).
2. Click **Register** in the sidebar (or follow the link on the home page).
3. Fill in:
   - **Username** — at least 3 characters
   - **Email**
   - **Password** — at least 6 characters
   - **Confirm password**
4. Click **Register**.

You are signed in immediately after registration — no email confirmation is required.

### Create your first campaign

1. Click **Campaigns** in the sidebar.
2. Click **New Campaign**.
3. Enter a campaign name and click **Save**.
4. Select the campaign in the list, then click **Open Campaign**.

You are now on the campaign detail page, where you can add characters, items, and skills.

---

## Navigating the app

### Sidebar

When you are logged in, the sidebar provides access to:

| Link | Purpose |
|------|---------|
| **Home** / **D&D Tracker** | Return to the app home page |
| **Campaigns** | View and manage all your campaigns |
| **Campaign modules** | Appear when you are viewing a campaign (see below) |
| **Log out** | Sign out of your account |

### Campaign modules

When you open a campaign, additional links appear under **Campaign modules**:

| Link | Purpose |
|------|---------|
| **Rollable Tables** | Opens in a **new browser tab** so you can roll while keeping the campaign page open |
| **Create NPC** | Open the NPC Generator |
| **View NPCs** | Browse and edit saved NPCs |

### Campaign detail layout

The campaign detail page is divided into three columns:

```
┌─────────────────┬──────────────────────┬─────────────────┐
│  Characters     │  Skills / Items /    │  Detail panel   │
│  Current NPCs   │  Unassigned tabs     │  (selected item │
│                 │                      │   or skill)     │
└─────────────────┴──────────────────────┴─────────────────┘
```

- **Left column** — Character list and **Current NPCs** (NPCs marked as active in the session)
- **Center column** — Tabbed lists for Skills, Items, or Unassigned loot
- **Right column** — Full details for the selected skill or item, including image upload

### Connection issues

DnDTracker uses a live connection to keep the page updated. If your connection drops, a **Rejoining the server…** dialog appears. Click **Retry** or **Resume** to reconnect. Unsaved work in open dialogs may be lost if the connection is interrupted for a long time — save changes promptly during play.

---

## Campaigns

The **Campaigns** page is your hub for all campaigns.

### Create a campaign

1. Go to **Campaigns**.
2. Click **New Campaign**.
3. Enter a name and click **Save**.

Campaign names must be **unique among your campaigns** (case-insensitive). For example, you cannot have both "Curse of Strahd" and "curse of strahd".

### Open a campaign

1. Click a campaign name in the list to select it.
2. Click **Open Campaign**.

### Rename a campaign

1. Select the campaign.
2. Click **Edit Campaign**.
3. Enter the new name and click **Save**.

### Delete a campaign

1. Select the campaign.
2. Click **Remove Campaign**.
3. Confirm by clicking **Yes**.

Deleting a campaign permanently removes all its characters, items, skills, and saved NPCs.

---

## Characters, skills, and items

Open a campaign to manage its characters and their associated data.

### Characters

#### Add a character

1. In the left column, click **Add**.
2. Enter a character name and click **Save**.

Character names must be unique within the campaign.

#### Edit or remove a character

1. Select the character in the list.
2. Click **Edit** to rename, or **Remove** to delete.

**Warning:** Removing a character also deletes **all items assigned to that character**. Skills for that character are removed as well.

### Skills

Skills are tracked per character. Use them for custom proficiencies, special abilities, training, or anything else you want to record.

#### Add a skill

1. Select a character.
2. Open the **Skills** tab.
3. Click **Add**.
4. Fill in:
   - **Skill name** (required)
   - **Description**
   - **Notes**
5. Click **Save**.

Skill names must be unique for that character.

#### View, edit, or remove a skill

1. Select a skill in the list — its details appear in the right column.
2. Click **Edit** or **Remove** as needed.

### Items

Items can belong to a specific character or sit in the **Unassigned** pool until you decide who gets them.

#### Item fields

| Field | Purpose |
|-------|---------|
| **Item name** | Required. The item's name |
| **Description** | What the item is |
| **Where found** | Location where the party found it |
| **When found** | Session, date, or story beat |
| **Current status** | Condition, ownership notes, or story state |
| **Notes** | Free-form DM notes |
| **Item image** | Optional portrait or photo (PNG, JPG, JPEG, or BMP, up to 5 MB) |

#### Add an item to a character

1. Select a character.
2. Open the **Items** tab.
3. Click **Add**, fill in the fields, and click **Save**.

#### Add an unassigned item

1. Open the **Unassigned** tab (no character selection required).
2. Click **Add**, fill in the fields, and click **Save**.

Use the unassigned pool for loot the party has found but not yet distributed.

#### Assign an item to a character

1. Open the **Unassigned** tab.
2. Select the item.
3. Click **Assign**.
4. Choose the character and confirm.

#### Unassign an item from a character

1. Select the character and open the **Items** tab.
2. Select the item.
3. Click **Unassign**.

The item moves back to the unassigned pool.

#### Copy an item

1. Select an item.
2. Click **Copy**.
3. Choose **Unassigned** or a specific character.

The copy includes the item's image.

#### Item images

- Click **Choose image** when creating or editing an item to upload a file.
- Click the image in the detail panel to view it full size.
- Click **Clear image** to remove the image from the item.

---

## Rollable tables

Rollable tables let you import random tables from CSV files and roll results during play. Rolled **Item** results can be added directly to a character or the unassigned pool; rolled **Skill** results can be added to a character.

> **Note:** Roll tables belong to your **user account**, not a single campaign. You access them from within a campaign, but the same imported tables are available across all your campaigns.

### Open rollable tables

From a campaign, click **Rollable Tables** in the sidebar (opens in a new tab) or navigate from the campaign modules section.

### Import a table

1. Click **Import CSV**.
2. Choose a **Table type**:
   - **Item** — rolled results can be added as campaign items
   - **Skill** — rolled results can be added as character skills
   - **Generic** — for reference only; results cannot be added to the campaign
3. Select a `.csv` file and confirm the import.

The table name is taken from the file name. See the [CSV format appendix](#appendix-csv-format-for-roll-tables) for file requirements.

### Roll on a table

1. Select a table from the list.
2. Review its rows in **Table contents**.
3. Click **Roll**.

The rolled row is highlighted in the table and shown in the **Roll result** panel.

### Add a rolled result to your campaign

After rolling on an **Item** table:

- **Add to Character** — pick a character; the item is created with the rolled name and descriptions
- **Add to Unassigned** — the item goes to the unassigned pool

After rolling on a **Skill** table:

- **Add Skill** — pick a character; the skill is created with the rolled name and descriptions

### Remove a table

1. Select the table.
2. Click **Remove Table** and confirm.

---

## NPC Generator and saved NPCs

### NPC Generator

Generate random NPCs for your campaign with identity, appearance, behaviour, story hooks, and DM notes.

1. Open a campaign and click **Create NPC** in the sidebar.
2. Click **Generate NPC**.
3. Review the generated sections:
   - **Identity** — name, ancestry, gender, age, occupation
   - **Appearance** — description and distinctive features
   - **Behaviour** — personality, mannerism, voice
   - **Story** — background, motivation, secret, current problem
   - **Game use** — quest hook, danger or complication
   - **DM summary** — a concise reference for play
   - **Image prompt** — text you can paste into an external image generator
4. Optionally edit the **Image prompt** textarea.
5. Use **Copy summary** or **Copy image prompt** to copy text to your clipboard.
6. When you are happy with the NPC, click **Save to campaign**.

After saving, you are taken to the **Saved NPCs** page.

Click **Generate another NPC** to create a new random character without leaving the page.

### Saved NPCs

View and manage NPCs you have saved to a campaign.

1. Open a campaign and click **View NPCs**.
2. Select an NPC from the list to see full details.

#### Upload a portrait

1. Select an NPC.
2. Click **Upload image** and choose a file (PNG, JPG, JPEG, or BMP, up to 5 MB).
3. To remove a portrait, click **Remove image**.

#### Edit an NPC

1. Select the NPC.
2. Click **Edit NPC**.
3. You can change:
   - **Name**
   - **Occupation**
   - **Location** — where the NPC is encountered in your world
   - **DM summary** — your play reference notes
   - **Show as current NPC** — when enabled, the NPC appears in the **Current NPCs** panel on the campaign page
4. Click **Save**.

#### Copy text for play

Use **Copy summary** or **Copy image prompt** on the detail view to copy text to your clipboard.

#### Remove a saved NPC

1. Select the NPC.
2. Click **Remove saved NPC** and confirm.

This also deletes any uploaded portrait.

#### Current NPCs

NPCs with **Show as current NPC** enabled appear in the left column of the campaign detail page under **Current NPCs**. Click an NPC's name to jump to their saved record.

---

## Import and export

Back up campaigns or move them between accounts using JSON export and import.

### Export a campaign

1. Go to **Campaigns**.
2. Select the campaign.
3. Click **Export Campaign**.

Your browser downloads a `{CampaignName}.json` file containing the campaign, its characters, items, and skills.

### Import a campaign

1. Go to **Campaigns**.
2. Click **Import Campaign**.
3. Select a JSON file exported from DnDTracker (maximum 10 MB).
4. Click **Import**.

Import is **all-or-nothing** — if any part of the file is invalid, nothing is imported.

### What is not included in export/import

| Data | Included in JSON? |
|------|-------------------|
| Campaign name | Yes |
| Characters | Yes |
| Items (text fields) | Yes |
| Skills | Yes |
| Item images | **No** — images are stored separately on the server |
| Saved NPCs | **No** |
| Roll tables | **No** — roll tables are per account, not per campaign |
| Provenance history | **No** |

After import, you may need to re-upload item images and recreate NPCs manually.

---

## Account management

### Log in

1. Click **Log in** in the sidebar.
2. Enter your **username or email** and **password**.
3. Click **Log in**.

### Log out

Click **Log out** in the sidebar.

### Reset your password

1. On the login page, click the forgot-password link (or go to **Account/ForgotPassword**).
2. Enter your email address and submit.
3. Check your email for a reset link.
4. Follow the link, enter a new password, and log in.

Password reset emails are sent via SendGrid. Check your spam folder if you do not receive the email promptly.

---

## Tips for running sessions

- **Use the Unassigned tab** for loot the party finds before deciding who carries it.
- **Open Rollable Tables in a new tab** (the sidebar link does this automatically) so you can roll while keeping the campaign page visible.
- **Mark session NPCs as Current** so they appear on the campaign page without hunting through the full NPC list.
- **Copy image prompts** from the NPC Generator into your preferred AI image tool, then upload the result as a portrait.
- **Export campaigns regularly** as a backup, especially before major changes or deletions.
- **Save dialogs promptly** — if your internet connection drops, unsaved form data may be lost.

---

## Troubleshooting

### "Campaign not found"

The campaign may have been deleted, or you may not have access to it. Return to **Campaigns** and verify the campaign still exists in your account.

### "A [entity] with that name already exists…"

Names must be unique within their scope:

| Entity | Unique within |
|--------|---------------|
| Campaign | Your account |
| Character | The campaign |
| Skill | The character |
| Item | The character or unassigned pool |

Choose a different name or edit the existing record.

### "Only PNG, JPG, JPEG, and BMP images are supported"

Upload an image in one of the supported formats. Other formats (GIF, WebP, SVG, etc.) are not accepted.

### Image too large

Item and NPC images must be **5 MB or smaller**. Resize or compress the image before uploading.

### NPC Generator unavailable

If you see a message that the NPC generator data could not be loaded, the feature is temporarily unavailable. Contact the site administrator if the problem persists.

### "Rejoining the server…"

Your browser lost its live connection to the app. Click **Retry** or **Resume**. If the problem continues, check your internet connection and refresh the page.

### Import failed

- Ensure the file is a valid DnDTracker export (`.json`).
- Check that the file is under 10 MB.
- If the file was edited manually, verify it is well-formed JSON.

---

## Appendix: CSV format for roll tables

Rollable tables are imported from CSV files with this structure:

```csv
Number,Name,PhysicalDescription,SpecialCharacteristics
1,Example Item,A worn leather pouch,Contains a hidden compartment
2,Another Item,A silver ring,Glows faintly in moonlight
```

### Requirements

| Rule | Detail |
|------|--------|
| Header row | Required — must be exactly: `Number,Name,PhysicalDescription,SpecialCharacteristics` |
| Columns | Four columns per data row |
| File size | Maximum 1 MB |
| File type | `.csv` |

### Table types

| Type | When to use |
|------|-------------|
| **Item** | Treasure, equipment, or other physical objects |
| **Skill** | Abilities, proficiencies, or training |
| **Generic** | Any table you only need to roll on for reference |

The **Name** column becomes the item or skill name when you add a rolled result to your campaign. **Physical Description** and **Special Characteristics** map to the description and notes fields.

---

## Getting help

For technical issues with the hosted app at [tracker.alanstirling.com](https://tracker.alanstirling.com), contact the site administrator.

For developers and self-hosters, see [DEPLOYMENT.md](DEPLOYMENT.md) and [CODE_DOCUMENTATION.md](CODE_DOCUMENTATION.md) in this repository.
