# Changelog

## Unreleased
- **Co-op Support (M6a/M6b, v1.1.0, unverified live):** Removed the host-only check in `SprinklerController` and the non-host block messages in the **Sprinklers** phone app so non-host players in co-op lobbies can water properties and apply additives from storage shelves, and added optional `SteamNetworkLib` (`UserLibs\`) P2P sync for spray mist/audio and shared watering & additive locks (`BroadcastLock` across both `WaterProperty` and `ApplyAdditiveToProperty`) across all players (pending live two-player verification).

## v1.0.0 — 2026-11-25
First full release supporting both **IL2CPP** (normal Steam branch) and **Mono** (`alternate` branch).

### Added
- **Overhead Sprinkler Head (`overhead_sprinkler_head`):** Cloned from the Full Spectrum Grow Light (`fullspectrumgrowlight`), snaps onto Suspension Racks (`FloorRack`), illuminates pots underneath, and mounts a procedural gunmetal pipe, cyan indicator ring, and brass nozzle underneath each placed head.
- **Oscar's Warehouse Listing:** Sold at Oscar's Store for $1,500 (configurable).
- **Automatic Per-Property Chaining & "Sprinklers" Phone App:** Lists every owned property with installed heads, displays live moisture bars and head/rack/pot totals, and supports one-tap **Water Now / Top Up** and **Water All**.
- **Fertilization / Additives Support:** Scans property storage shelves (`PlaceableStorageEntity`) for **Fertilizer**, **PGR**, and **Speed Grow**, consumes 1 bottle per eligible planted pot, and applies it via `GrowContainer.ApplyAdditive_Server` with live stock counts in the Phone App.
- **Spray Effects & Equalized Audio:** Downward cone mist particles (blue for water, color-coded bursts for additives) and spatial vanilla sprinkler sounds scaled to the player's Master × FX volume via `AudioManager.MainGameMixer`.
- **MelonPreferences Config:** `[OverheadSprinkler]` settings for purchase price, spray duration, particle toggle, sound toggle, and volume multiplier.

## Pre-release milestones
- **M0:** Static research and `RackProbe` diagnostic tool.
- **M1–M3:** Mono proof-of-concept, Oscar's Store listing, Phone App dashboard redesign, and property-storage additive delivery.
- **M4:** Spray particles, equalized audio, `MelonPreferences`, IL2CPP dual-target build, and IL2CPP `GetInstanceID()` deduplication fix.
