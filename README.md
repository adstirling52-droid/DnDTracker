# DnDTracker

A web application for Dungeon Masters to track D&D campaigns, characters, items, skills, and roll tables during play.

The live app is hosted at [tracker.alanstirling.com](https://tracker.alanstirling.com).

**User guide:** see [DnDTracker.Web/Data/UserGuide/USER_GUIDE.md](DnDTracker.Web/Data/UserGuide/USER_GUIDE.md) for how to use the app (also available at `/user-guide` in the running app).

## Web app

The application lives in `DnDTracker.Web/`. It is an ASP.NET Core Blazor Server app with SQL Server persistence and ASP.NET Core Identity for multi-user access. Each user's data is kept separate.

Open `DnDTracker.Web/DnDTracker.Web.slnx` in Visual Studio to build and run locally.

**Deployment:** see [docs/DEPLOYMENT.md](docs/DEPLOYMENT.md) for Azure VM + IIS + SQL Server setup.
