# Nexus Mods Release Guide — Overhead Sprinkler v1.0.0

Everything below is ready to copy-paste when creating the mod page on Nexus Mods.

---

## Mod Page Fields

- **Mod Name:** `Overhead Sprinkler`
- **Version:** `1.0.0`
- **Category:** `Gameplay` (or `Items` / `Automation`)
- **Author:** `Trooper-code`
- **Short Summary (max 255 chars):**
  > Rack-mounted Full Spectrum Grow Light with an integrated overhead sprinkler nozzle. Automatically chains per property and lets you water pots and apply Fertilizer, PGR, or Speed Grow from property storage via a Sprinklers phone app.

---

## Requirements (Nexus "Requirements" Tab)

1. **MelonLoader** (`v0.7.3` or newer) — required
2. **S1API (Forked by ifBars)** (`v3.2.3` or newer) — required (use the **IL2CPP** build for the normal Steam branch, or the **Mono** build for `alternate`)

---

## Files to Upload

1. **Main File:** `dist\OverheadSprinkler-v1.0.0-IL2CPP.zip`
   - **File Name on Nexus:** `Overhead Sprinkler (IL2CPP - Normal Steam Branch)`
   - **Version:** `1.0.0`
   - **Description:** `For the default Schedule I Steam branch (IL2CPP). Extract into your Schedule I game folder (places OverheadSprinkler.IL2CPP.dll in Mods\). Requires MelonLoader 0.7.3+ and S1API (Forked by ifBars) IL2CPP 3.2.3+.`
2. **Optional File:** `dist\OverheadSprinkler-v1.0.0-Mono.zip`
   - **File Name on Nexus:** `Overhead Sprinkler (Mono - Alternate Beta Branch)`
   - **Version:** `1.0.0`
   - **Description:** `For the Schedule I 'alternate' Mono beta branch only. Extract into your Schedule I game folder (places OverheadSprinkler.Mono.dll in Mods\). Requires MelonLoader 0.7.3+ and S1API (Forked by ifBars) Mono 3.2.3+.`

---

## Full Description (Copy-Paste for Nexus Page)

```markdown
### Overhead Sprinkler (`v1.0.0`)

Keep your grow room floors clear! **Overhead Sprinkler** adds an **Overhead Sprinkler Head** sold by **Oscar at the Warehouse ($1,500)** that snaps directly onto **Suspension Racks** just like a Full Spectrum Grow Light—giving your plants both light and overhead irrigation from a single fixture.

#### Features
- **Rack-Mounted Grow Light + Sprinkler Combo:** Cloned from the Full Spectrum Grow Light so it lights the pots beneath it and keeps your floor tiles completely open.
- **Distinct Overhead Hardware:** Each placed head mounts a gunmetal manifold pipe, cyan status ring, and brass nozzle underneath so you can tell it apart from standard grow lights.
- **Automatic Per-Property Chaining:** Every Overhead Sprinkler Head placed inside a property (Bungalow, Barn, Warehouse, etc.) links together automatically—no wiring UI needed.
- **"Sprinklers" In-Game Phone App:** Manage all of your owned properties from anywhere on your phone:
  - Live moisture bars plus head, rack, and pot counts per property
  - **Water Now / Top Up** for a single property, or **Water All** across every property at once
- **Property-Storage Fertilization / Additives:**
  - Stock **Fertilizer**, **PGR**, or **Speed Grow** on any storage shelf inside the property
  - Tap the additive button in the **Sprinklers** app to consume 1 bottle per eligible planted pot and apply it automatically (skips empty pots, fully grown plants, and pots that already have that additive)
- **Spray Effects & Equalized Audio:** Downward nozzle mist (blue for water, color-coded bursts for additives) and spatial sprinkler sounds balanced to your in-game Master × FX volume.
- **Configurable:** Adjust price, spray duration, particles, sound toggle, and volume in `UserData/MelonPreferences.cfg` under `[OverheadSprinkler]`.
- **Save-Safe:** Built on Schedule I's native item/grid system; removing the mod will not corrupt your save.

#### Installation (Normal Steam Branch / IL2CPP)
1. Install **MelonLoader 0.7.3+** and launch Schedule I once to the main menu.
2. Install **S1API (Forked by ifBars) 3.2.3+** (**IL2CPP** build: `S1API.Il2Cpp.MelonLoader.dll`) into `Schedule I\Mods\`.
3. Extract **`OverheadSprinkler-v1.0.0-IL2CPP.zip`** into your `Schedule I` folder (so `OverheadSprinkler.IL2CPP.dll` lands in `Schedule I\Mods\`).
4. Visit **Oscar at the Warehouse** to purchase Overhead Sprinkler Heads, hang them on Suspension Racks, and open the **Sprinklers** app on your phone!

*(If you play on Steam's `alternate` beta branch, use **`OverheadSprinkler-v1.0.0-Mono.zip`** with the **Mono** build of S1API instead.)*
```

---

## Changelog for Nexus (`v1.0.0`)

- Initial public release for Schedule I (IL2CPP & Mono).
- Added Overhead Sprinkler Head ($1,500 at Oscar's Store) with procedural pipe & brass nozzle visuals.
- Added "Sprinklers" in-game phone app with per-property chaining, Water / Water All, and live moisture bars.
- Added Fertilizer / PGR / Speed Grow delivery from property storage shelves.
- Added nozzle spray mist particles, equalized spatial sprinkler audio, and `MelonPreferences` configuration.
