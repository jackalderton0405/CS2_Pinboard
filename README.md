# Pinboard

A quality-of-life mod for **Cities: Skylines II** that lets you favourite ("pin") any asset and organise your pins into named collections and filters — so the assets you actually use are always a click away instead of buried in the build menus.

> **Status:** Beta. Feedback and bug reports are welcome.

## Features

- **Pin any asset** — buildings, props, trees, network pieces, and more. Pin the currently selected asset straight from the tool options panel, or remove it just as quickly.
- **Collections** — group your pins into as many named collections as you like (e.g. *Downtown*, *Suburbs*, *Industrial*).
- **Filters** — within a collection, create funnel-style filters to slice your pins into sub-groups (e.g. *Parks*, *Skyscrapers*). A pin can belong to multiple filters.
- **Quick access panel** — a floating button opens the Pinboard panel; click any pin to instantly activate that asset's placement tool.
- **Search & sort** — filter your pins by name and sort A→Z / Z→A.
- **Tool-options integration** — pin the selected asset and assign it to filters directly from the in-game tool options, next to the vanilla controls.
- **Persistent & safe** — your pins are saved to disk with atomic writes and automatic backup recovery, so they survive across sessions.

## How it works

The hierarchy is simple:

```
Collection
 ├── Pins        (your favourited assets)
 └── Filters     (sub-groups; each pin can be in any number of filters)
```

Open the panel with the Pinboard button (top-right), pick or create a collection, and start pinning. Use the **+ Pin** button in the tool options when an asset is selected, and the funnel button to assign it to filters.

## Installation

**From PDX Mods (recommended):** subscribe in-game via the Paradox Mods browser. *(Coming with the public release.)*

**Manual:** copy the contents of the release into:

```
%LocalAppData%Low\Colossal Order\Cities Skylines II\Mods\Pinboard\
```

(The `Pinboard.dll` and `Pinboard.mjs` must sit together in that folder.)

## Your data

Pins are stored locally, separate from your save games, at:

```
%LocalAppData%Low\Colossal Order\Cities Skylines II\ModsData\Pinboard\favourites.json
```

This file is created on first run — a fresh install starts with a single empty collection. Writes are atomic, with a `.bak` fallback if the main file is ever corrupted. Nothing is uploaded anywhere.

## Building from source

The mod is a C# backend plus a TypeScript/React UI.

**Requirements**
- Cities: Skylines II modding toolchain (sets the `CSII_TOOLPATH` environment variable used by the build)
- .NET SDK
- Node.js 18+

**Build the UI**

```powershell
cd Pinboard/UI
npm install
npm run build      # outputs dist/Pinboard.mjs
```

**Build the mod**

Open `Pinboard.slnx` in Visual Studio and build, or run `dotnet build`. The build's `DeployUI` step copies the prebuilt `Pinboard.mjs` into the deployed mod folder. For quick UI-only iteration, `npm run deploy` rebuilds and copies just the `.mjs` to your Mods folder.

> No hot-reload — restart the game after any change.

## Project layout

```
Pinboard/
├── Data/FavouritesData.cs        Data model (collections, filters, pins)
├── Systems/
│   ├── FavouritesService.cs      JSON load/save + CRUD
│   └── PinboardUISystem.cs       C# ↔ UI bindings
├── UI/src/                       TypeScript/React frontend
│   ├── index.tsx                 Mod registration
│   └── mods/                     Panel, tool-options overlay, button, icons
├── Mod.cs                        Mod entry point
└── Setting.cs                    Options/locale
```

## License

Released under the [MIT License](LICENSE).
