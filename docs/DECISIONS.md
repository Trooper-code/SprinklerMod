# Decisions

## D4 · 2026-11-19 · Keep the repo local
**Decided:** The repo lives only on Josh's PC. No GitHub remote, no CI, no PRs. Antigravity commits to local
branches and Claude reviews them through the connected folder.
**Rejected:** A public GitHub repo from day one (the work sits right next to game files and decompiled code, and a
mistake there would be public).
**Why:** Josh's choice, because the project works with game files.
**Revisit if:** the mod is ready to share. The never-commit-game-files rules still apply, so the repo can be
published later as-is (add a remote, restore CI from the coordinator-skills templates).
**Supersedes:** nothing

## D3 · 2026-11-19 · Water automatically, not on interact
**Decided:** The head tops up any pot under its rack whose water drops below a set level. No button to press.
**Rejected:** Interact to activate, like the vanilla sprinklers (a head up on the rack is awkward to aim at, and it
wouldn't help Botanists). A wall valve linking heads (extra item and UI for little gain; can be added later).
**Why:** Josh's choice. Also, players report Botanists can't use the vanilla sprinklers (unverified, Steam forum
post, ~April 2026), so automatic watering is the only way for the mod to help staff-run grows.
**Revisit if:** auto-checking costs noticeable performance, or players ask for manual control.
**Supersedes:** nothing

## D2 · 2026-11-19 · One head per Suspension Rack, covering every pot under that rack
**Decided:** Each head covers the pots under the single rack it hangs on. Racks are never linked together.
**Rejected:** One head per light-sized area (more items to place for no real benefit). Linking neighbouring racks
into one system (each rack is its own placed item; joining them is overly complicated).
**Why:** Josh's choice.
**Revisit if:** the game turns out to treat side-by-side racks as one object.
**Supersedes:** nothing

## D1 · 2026-11-19 · Hang the head on the Suspension Rack instead of free ceiling mounting
**Decided:** The head attaches to the Suspension Rack using the same placement grow lights use (planned: build it
by cloning a grow light with S1API).
**Rejected:** Free ceiling mounting (needs custom placement code: raycasts, ghost preview, validation). A
floor-standing pole (works, but doesn't match the look Josh wants).
**Why:** Grow lights already snap onto the rack in the vanilla game (confirmed in Josh's screenshot, 2026-11-19),
so reusing that placement avoids the hardest part of the mod. Whether S1API can clone a grow light this way is
unverified until M0.
**Revisit if:** M0 finds rack attachment is hard-wired to grow lights in a way a clone can't use.
**Supersedes:** nothing
