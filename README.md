# Overhead Sprinkler for Schedule I

A MelonLoader mod for **Schedule I** that adds a rack-mounted **Overhead Sprinkler Head**. Hang it on any Suspension Rack just like a Full Spectrum Grow Light, and water or fertilize entire properties with one tap from the in-game **Sprinklers** phone app.

## Features

- **Grow Light + Sprinkler in One Fixture:** Snaps onto Suspension Racks and lights the pots underneath without taking up floor space.
- **Distinct Overhead Visuals:** Each placed head mounts a gunmetal supply pipe, cyan indicator ring, and brass nozzle underneath so you can easily tell it apart from standard grow lights.
- **Automatic Per-Property Chaining:** Every head placed inside a property links together automatically—no wiring or manual setup required.
- **"Sprinklers" In-Game Phone App:** Manage all owned properties from anywhere:
  - Live moisture bars plus head, rack, and pot counts per property
  - **Water Now / Top Up** for one property, or **Water All** across every property at once
- **Property-Storage Fertilization:** Stock **Fertilizer**, **PGR**, or **Speed Grow** on storage shelves inside the property. The phone app shows live bottle counts, consumes 1 bottle per eligible planted pot, and skips empty pots, fully grown plants, and pots that already have that additive.
- **Spray Mist & Balanced Audio:** Downward mist particles (blue for water, color-coded for additives) with spatial sprinkler audio scaled to your in-game Master × FX volume.
- **Save-Safe & Dual-Runtime:** Uses Schedule I's native item and grid system, and supports both the default **IL2CPP** Steam branch and the `alternate` **Mono** beta branch.

## Requirements

- **Schedule I** (tested on `v0.4.7f12`)
- **MelonLoader** `0.7.3+` — [Download](https://melonwiki.xyz/)
- **S1API (Forked by ifBars)** `3.2.3+` — [Releases](https://github.com/ifBars/S1API/releases) / Thunderstore
- **SteamNetworkLib (by ifBars)** `1.6.0+` *(optional, recommended for co-op)* — [Releases](https://github.com/ifBars/SteamNetworkLib/releases) / Thunderstore. Syncs spray mist, audio, and the watering/additive lock across players; single-player works fine without it.

## Installation

Grab the zip that matches your Steam branch from the [Releases](https://github.com/Trooper-code/SprinklerMod/releases) page:

### Default Steam Branch (IL2CPP)
1. Install **MelonLoader 0.7.3+** into your Schedule I game folder and launch the game once to the main menu.
2. Download the **IL2CPP** build of **S1API (Forked by ifBars)** (`S1API.Il2Cpp.MelonLoader.dll`) and place it in `<Schedule I>\Mods\`.
3. *(Optional, for synced co-op spray mist/audio & shared lock)* Download `SteamNetworkLib-IL2Cpp.dll` and place it in `<Schedule I>\UserLibs\`.
4. Extract `OverheadSprinkler-v1.0.0-IL2CPP.zip` into your `<Schedule I>` folder so `OverheadSprinkler.IL2CPP.dll` is in `<Schedule I>\Mods\`.

### `alternate` Beta Branch (Mono)
1. Switch Schedule I to the `alternate` beta in Steam and install **MelonLoader 0.7.3+**.
2. Place the **Mono** build of **S1API (Forked by ifBars)** (`S1API.dll`) in `<Schedule I>\Mods\`.
3. *(Optional, for synced co-op spray mist/audio & shared lock)* Place `SteamNetworkLib-Mono.dll` in `<Schedule I>\UserLibs\`.
4. Extract `OverheadSprinkler-v1.0.0-Mono.zip` into your `<Schedule I>` folder so `OverheadSprinkler.Mono.dll` is in `<Schedule I>\Mods\`.

## How to Use

1. Visit **Oscar** at the **Warehouse** and buy **Overhead Sprinkler Heads** ($1,500 each by default).
2. Place them onto **Suspension Racks** above your pots.
3. Open the **Sprinklers** app on your in-game phone to monitor moisture and water individual properties or all properties at once.
4. *(Optional)* Place **Fertilizer**, **PGR**, or **Speed Grow** on storage shelves inside the same property to apply them from the app.

## Multiplayer

- **Everyone needs the mod:** Every player in the co-op lobby must have **Overhead Sprinkler** installed (same version, matching each player's Steam branch—IL2CPP or Mono).
- **Anyone can trigger it:** Both the host and non-host friends can use the **Sprinklers** phone app to water properties and apply additives from property storage shelves.
- **Synced mist, sound & watering/additive lock:** Drop `SteamNetworkLib-IL2Cpp.dll` (or `SteamNetworkLib-Mono.dll` on `alternate`) into `<Schedule I>\UserLibs\` on each player's PC so everyone sees the spray mist, hears the sprinkler sound, and shares the property lock during watering and additive bursts. Without `SteamNetworkLib`, watering and additives still sync via the game's servers, and mist/sound simply stay local to whoever pressed the button. *(Co-op sync is pending live two-player verification.)*

## Configuration

After launching the game once with the mod installed, edit `<Schedule I>\UserData\MelonPreferences.cfg` under `[OverheadSprinkler]`:

| Setting | Default | Description |
|---|---|---|
| `HeadPurchasePrice` | `1500.0` | Purchase price of the Overhead Sprinkler Head at Oscar's Warehouse |
| `SprayDurationSeconds` | `4.5` | Length of a watering cycle in seconds (`0` for instant fill) |
| `EnableSprayParticles` | `true` | Show downward water and additive mist particles under each head |
| `EnableSpraySound` | `true` | Play sprinkler click and spray audio during activation |
| `SoundVolume` | `0.7` | Volume multiplier (`0.0`–`1.0`), scaled by your in-game Master × FX audio settings |

## Troubleshooting

- **Mod doesn't load or crashes on startup:** Make sure both **Overhead Sprinkler** and **S1API** match your Steam branch (`IL2CPP` for the default branch, `Mono` for `alternate`). Don't put both DLLs in `Mods\` at the same time.
- **Additives show `0` in the phone app:** Bottles must be placed on storage shelves inside the same property as the sprinkler heads.

## Building from Source

1. Install the **.NET 10 SDK** (or any .NET 6+ SDK supporting `netstandard2.1` and `net6.0`).
2. Copy `Directory.Build.props.user.example` to `Directory.Build.props.user` at the repo root and set your local paths:
   - `ScheduleIManagedDir` → `<Schedule I Mono>\Schedule I_Data\Managed`
   - `ScheduleIIl2CppDir` → `<Schedule I>\MelonLoader\Il2CppAssemblies`
   - `ScheduleIMelonNet6Dir` → `<Schedule I>\MelonLoader\net6`
   - *(Optional)* `SteamNetworkLibIl2CppPath` / `SteamNetworkLibMonoPath` → paths to `SteamNetworkLib-IL2Cpp.dll` / `SteamNetworkLib-Mono.dll` to enable P2P effect & lock sync in that target build.
3. Build both targets:
   ```powershell
   dotnet build src/OverheadSprinkler/OverheadSprinkler.csproj -c Release
   ```
   Outputs:
   - `src/OverheadSprinkler/bin/Release/net6.0/OverheadSprinkler.IL2CPP.dll`
   - `src/OverheadSprinkler/bin/Release/netstandard2.1/OverheadSprinkler.Mono.dll`

## Research & Documentation

Notes on Schedule I's `FloorRack`, `ProceduralGridItem`, `GrowContainer`, storage shelves, and `S1API` are documented in [docs/research/INDEX.md](docs/research/INDEX.md).

## License & Legal

This repository contains only original source code and documentation—no copyrighted game files, decompiled code, or proprietary assets. Schedule I is made by TVGS.
