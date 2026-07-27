# gregMod.MoreSpools

> Adds configurable long cable spools to **Data Center**, with regular and RGB variants.

[![Discord](https://img.shields.io/discord/1392073682133848075?style=for-the-badge&logo=discord&logoColor=white&label=Discord)](https://discord.gg/greg)
[![gregFramework](https://img.shields.io/badge/gregFramework-Website-blue?style=for-the-badge)](https://gregframework.eu)
[![License](https://img.shields.io/badge/License-Apache%202.0-green?style=for-the-badge)](./LICENSE)
[![Version](https://img.shields.io/badge/Version-1.1.1-orange?style=for-the-badge)]()
[![GameVersion](https://img.shields.io/badge/Game%20Version-1.1.0-yellow?style=for-the-badge)]()
[![Unity](https://img.shields.io/badge/Unity-6000.4.12f1-black?style=for-the-badge&logo=unity&logoColor=white)]()

## Links

- **Repository:** [github.com/mleem97/gregMod.MoreSpools](https://github.com/mleem97/gregMod.MoreSpools)
- **Discord / Support:** [discord.gg/greg](https://discord.gg/greg)
- **Website:** [gregframework.eu](https://gregframework.eu)

## Overview

**gregMod.MoreSpools** adds configurable long cable spools to the Data Center shop without requiring a recompile for configuration changes.

## Features

- Additional spool lengths available in the shop
- Each length is offered as a **regular** and an **RGB** (custom colour) variant
- Lengths are fully configurable in `UserData/LargerSpools.json`

## Dependencies

- [MelonLoader](https://melonwiki.xyz/) v0.7.2 or newer

## Installation

1. Install MelonLoader for Data Center if you haven't already
2. Copy `gregMod.MoreSpools.dll` into `Data Center/Mods/`
3. Launch the game — `UserData/LargerSpools.json` is created automatically on first run
4. Edit the JSON to add or change lengths, then restart the game

## Configuration

On first launch the mod writes `UserData/LargerSpools.json` with default values:

```json
{
  "cable_types": {
    "0": [
      { "length_m": 500,  "price_multiplier": 2.0 },
      { "length_m": 1000, "price_multiplier": 3.0 },
      { "length_m": 2000, "price_multiplier": 5.0 }
    ]
  }
}
```

The key is the vanilla `CableSpinner.cableType` value.  
You can add entries for other cable types the same way:

```json
{
  "cable_types": {
    "0": [
      { "length_m": 500,  "price_multiplier": 2.0 },
      { "length_m": 1000, "price_multiplier": 3.0 }
    ],
    "1": [
      { "length_m": 500,  "price_multiplier": 2.0 },
      { "length_m": 2000, "price_multiplier": 5.0 }
    ]
  }
}
```

| Field              | Description                                                        |
|--------------------|--------------------------------------------------------------------|
| `length_m`         | Spool length in metres (must be > 0)                               |
| `price_multiplier` | Price relative to the vanilla spool of that cable type             |

Each cable type supports up to **16** custom lengths.  
Cable types not listed in the JSON receive no additional lengths.

> **Important:** Once a save has been created, **do not change existing `length_m` values**.  
> The save GUID is derived from the length; changing it will break existing saves for that spool.  
> To add new lengths, append entries — never reorder or remove existing ones.

## Notes

- Vanilla already ships 100 m, 200 m, and 500 m spools — only add lengths not already covered by vanilla for the relevant cable types
- The RGB variant lets players pick a custom hex colour via the vanilla colour picker

## Build from Source

Requirements:

- .NET 6 SDK
- local Data Center / MelonLoader installation

```bash
dotnet build -c Release
```

Release output: `bin/Release/net6.0/gregMod.MoreSpools.dll`

## Project Structure

```
gregMod.MoreSpools/
├── src/
│   ├── Core.cs             # MelonLoader entry point and shop integration
│   ├── Config.cs           # JSON configuration loading
│   ├── SpinnerDefinitions.cs # Custom spool definitions
│   └── Patches.cs          # Game Harmony patches
├── references/             # Current game and MelonLoader assemblies
├── gregMod.MoreSpools.csproj
└── README.md
```

## Credits

- Original implementation: [leoms1408](https://github.com/leoms1408)
- gregMod rebranding and current game update: [TeamGreg Modding](https://github.com/teamGregModding)

## License

See the project source and original distribution terms before redistribution.

## 🚀 Join the gregFramework Team!

Contributions, testing, documentation, and feedback are welcome in the [greg Discord](https://discord.gg/greg).
