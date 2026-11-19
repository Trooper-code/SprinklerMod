# Overhead Sprinkler for Schedule I

A MelonLoader mod for **Schedule I** that adds an **Overhead Sprinkler Head**. You hang it on a Suspension Rack,
the same way you hang grow lights, and it waters every pot under that rack **automatically** when the pots start
running dry.

> **Status: in development, not playable yet.** See [docs/MILESTONES.md](docs/MILESTONES.md) for progress.

## Planned features
- **Hangs on the Suspension Rack.** Snaps on like a grow light. No new frame to build.
- **One head per rack.** It covers every pot under the rack it's attached to.
- **Waters automatically.** When a pot under it drops below a set water level, it tops that pot up. You (and your
  Botanists) never need to water those pots by hand.
- **Settings** for the auto-water level and other options, in MelonLoader's preferences file.
- **Works on both game branches:** the normal (IL2CPP) branch and the `alternate` (Mono) branch.

## How it works (for modders)
The game already has the pieces: grow lights attach to the Suspension Rack and affect the pots under it, and the
Pot Sprinkler and Big Sprinkler know how to add water to a pot. This mod combines them. What we've learned about the
game's code is written up in [docs/research/](docs/research/INDEX.md), so others can build on it.

## Where this lives
This repo is kept **local** for now (see `docs/DECISIONS.md` D4). It's written so it could be published later
without changes: it never contains game files.

## Building from source
Build instructions will be added once the first code milestone (M1) lands. You'll need your own copy of
Schedule I. This repo never contains game files: the project points to the DLLs in your own install.

## Legal
This project contains only original code and documentation. It doesn't include any of the game's files, code or
assets. Schedule I is made by TVGS.
