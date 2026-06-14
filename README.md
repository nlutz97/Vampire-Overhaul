# VampireOverhaul - Local Mod Folder

This is your local development copy of the Bannerlord Vampire Mod.

## Recommended Location
Place this entire folder here on your machine:

`Documents\Mount & Blade II Bannerlord\Mods\VampireOverhaul\`

(Windows path: `%USERPROFILE%\Documents\Mount & Blade II Bannerlord\Mods\VampireOverhaul\`)

**Why this location?**
- The game automatically loads mods from the `Mods` folder in Documents.
- It survives game updates and Steam Workshop syncs better than putting it directly in the game install `Modules` folder.
- Easier permissions for writing during development.

Alternative (advanced): You can also place it in your game install folder under `Modules\VampireOverhaul\`, but Documents is preferred for active development.

## Current Structure
- `SubModule.xml` — Core mod declaration (Harmony dependency first, game types, version). **Do not delete or rename this.**
- `ModuleData/` — XML overrides go here (safe copies only — never edit Native originals).
- `bin/Win64_Shipping_Client/` — Compiled .dll will go here once we have C# code.
- `Languages/` and `GUI/` — Reserved for future localization and UI assets.

## How to Install / Test
1. Make sure Bannerlord.Harmony is installed (usually via Vortex, Nexus, or manual — it's a hard dependency).
2. Copy or move this `VampireOverhaul` folder into your `Mods` directory (see path above).
3. Launch Bannerlord → Mods menu → Enable "VampireOverhaul".
4. Start a new campaign or custom battle to test.

## Development Workflow (with our project hub)
- Our single source of truth is in the artifacts workspace: `/home/workdir/artifacts/VAMPIRE_MOD_PROJECT_CENTRAL.md`
- When I provide new code or XML, I'll either:
  - Give you full copy-paste content here, or
  - Add it to the mod_template here so you can download the updated files.
- Recommended: Keep this local folder in sync by copying updated files from the hub, or set up a simple script / Git if you want professional versioning.

## Next Steps (from our pipeline)
We're currently in **Phase 0 → Phase 1 transition**.
Next up: Flesh out the foundation (custom Blood Lust component + basic Harmony patch skeleton).

Once this folder is in place on your machine, tell me and we'll immediately drop the first real C# code and start building the resource system.

## Notes
- Folder name **must** match the `<Id>` in SubModule.xml.
- If you want to rename the mod (e.g., to "BloodSovereign" or "CrimsonFangs"), just rename the folder + update the three places in SubModule.xml (Name, Id, and any future DLL references).
- Always back up your save before heavy testing.

Let's build something great. Questions? Ready when you are.