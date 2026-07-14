# DnDTracker

A learning project in C# for tracking D&D campaigns, characters, items, skills, roll tables, and item provenance.

## Projects in this repository

| Folder | Purpose | Status |
|--------|---------|--------|
| `DnDTracker/` | Windows desktop app (WPF) | Working |
| `DnDTracker.Web/` | Online web app (ASP.NET Core) | In progress |

## Desktop app

The desktop version is a WPF application that stores data locally on your PC as JSON files under `%LocalAppData%\DnDTracker\`.

Open `DnDTracker/DnDTracker.slnx` in Visual Studio to build and run it.

## Web app

The web version is being built in `DnDTracker.Web/`. It will support multiple users, with each user's data kept separate, and will be hosted on Azure IIS at www.alanstirling.com.
