# Overhead Sprinkler for Schedule I

A MelonLoader mod for **Schedule I** that adds an **Overhead Sprinkler Head**. Hang it on a Suspension Rack just like a Full Spectrum Grow Light, and control watering and fertilization across entire properties with one tap from the in-game **Sprinklers** phone app.

## Features
- **Hangs on the Suspension Rack:** Cloned from the Full Spectrum Grow Light (`fullspectrumgrowlight`) so it snaps onto any Suspension Rack slot and lights the pots underneath—no floor space wasted.
- **Distinct Overhead Nozzle Visuals:** Each placed head mounts a gunmetal supply pipe, cyan indicator ring, and brass sprinkler nozzle underneath so you can tell it apart from standard grow lights.
- **Automatic Per-Property Chaining:** Every Overhead Sprinkler Head placed in a property activates together automatically—no linking or wiring required.
- **"Sprinklers" In-Game Phone App:** Accessible from anywhere on your phone:
  - Lists every owned property with heads installed (head count, rack count, pot count, and live moisture bar).
  - **Water Now / Top Up** waters every pot under every sprinkler-equipped rack in that property; **Water All** waters every property at once.
  - **Additives (Fertilizer, PGR, Speed Grow):** Shows live bottle counts from storage shelves inside that property, consumes 1 bottle per eligible planted pot, and applies it automatically.
- **Spray Particles & Equalized Audio:** Downward water mist (and color-coded additive bursts) plus vanilla sprinkler click/spray sounds scaled to your in-game Master × FX volume.
- **Oscar's Warehouse Shop Listing:** Sold by **Oscar** at the Warehouse for **$1,500** (configurable).
- **Save-Safe & Cross-Runtime:** Builds for both the normal **IL2CPP** Steam branch and the `alternate` **Mono** branch.

## Requirements
- **Schedule I** (v0.4.7f12 tested)
- **MelonLoader** `0.7.3`+ ([MelonLoader Installer](https://melonwiki.xyz/))
- **S1API (Forked by ifBars)** `3.2.3`+ ([Releases](https://github.com/ifBars/S1API/releases) / Thunderstore)

## Installation

### Normal Steam Branch (IL2CPP)
1. Install **MelonLoader 0.7.3** into your Schedule I game folder and launch the game once to the main menu.
2. Download the **IL2CPP** build of **S1API (Forked by ifBars)** (`S1API.Il2Cpp.MelonLoader.dll`) and place it in `<Schedule I>\Mods\`.
3. Copy `OverheadSprinkler.IL2CPP.dll` into `<Schedule I>\Mods\`.
4. Launch the game and visit **Oscar** at the Warehouse to buy **Overhead Sprinkler Heads** ($1,500).

### `alternate` Beta Branch (Mono)
1. Switch Schedule I to the `alternate` beta in Steam and install **MelonLoader 0.7.3**.
2. Place the **Mono** build of **S1API (Forked by ifBars)** (`S1API.dll`) in `<Schedule I>\Mods\`.
3. Copy `OverheadSprinkler.Mono.dll` into `<Schedule I>\Mods\`.

## Configuration (`UserData/MelonPreferences.cfg`)
After launching once, an `[OverheadSprinkler]` section is created in `<Schedule I>\UserData\MelonPreferences.cfg`:

| Setting | Default | Description |
|---------|---------|-------------|
| `HeadPurchasePrice` | `1500.0` | Purchase price of the Overhead Sprinkler Head at Oscar's Warehouse |
| `SprayDurationSeconds` | `4.5` | Duration of a 5-segment watering cycle in seconds (`0` for instant fill) |
| `EnableSprayParticles` | `true` | Show downward water/additive mist particles under each head |
| `EnableSpraySound` | `true` | Play sprinkler click and water spray sounds during activation |
| `SoundVolume` | `0.7` | Volume multiplier (`0.0`–`1.0`) scaled by your in-game Master × FX audio settings |

## Building from Source
1. Install the **.NET 10 SDK** (or .NET 6+ SDK supporting `netstandard2.1` and `net6.0`).
2. Copy `Directory.Build.props.user.example` to `Directory.Build.props.user` at the repo root and point:
   - `ScheduleIManagedDir` → `<Schedule I Mono>\Schedule I_Data\Managed`
   - `ScheduleIIl2CppDir` → `<Schedule I>\MelonLoader\Il2CppAssemblies`
   - `ScheduleIMelonNet6Dir` → `<Schedule I>\MelonLoader\net6`
3. Build both targets:
   ```powershell
   dotnet build src/OverheadSprinkler/OverheadSprinkler.csproj -c Release
   ```
   Outputs:
   - `src/OverheadSprinkler/bin/Release/netstandard2.1/OverheadSprinkler.Mono.dll`
   - `src/OverheadSprinkler/bin/Release/net6.0/OverheadSprinkler.IL2CPP.dll`

## Research & Documentation
Everything discovered about Schedule I's `FloorRack`, `ProceduralGridItem`, `GrowContainer`, storage shelves, and `S1API` is documented in [docs/research/INDEX.md](docs/research/INDEX.md).

## Legal
This project contains only original code and documentation. It never includes game files, decompiled code, or proprietary assets. Schedule I is made by TVGS.
