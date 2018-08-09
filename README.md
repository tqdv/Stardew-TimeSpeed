This repository contains my mods for [Stardew Valley](http://stardewvalley.net/):

* [All Crops All Seasons](AllCropsAllSeasons) ([Nexus](http://www.nexusmods.com/stardewvalley/mods/170))
* [All Professions](AllProfessions) ([Nexus](http://www.nexusmods.com/stardewvalley/mods/174))
* [Instant Grow Trees](InstantGrowTrees) ([Nexus](http://www.nexusmods.com/stardewvalley/mods/173))
* [Recatch Legendary Fish](RecatchLegendaryFish) ([Nexus](http://www.nexusmods.com/stardewvalley/mods/172))
* ~~[Skull Cave Saver](_archived/SkullCaveSaver)~~ (archived)
* [TimeSpeed](TimeSpeed) ([Nexus](http://www.nexusmods.com/stardewvalley/mods/169))

## Compiling the mods
Installing stable releases from Nexus Mods is recommended for most users. If you really want to
compile the mod yourself, read on.

These mods use the [crossplatform build config](https://www.nuget.org/packages/Pathoschild.Stardew.ModBuildConfig)
so they can be built on Linux, Mac, and Windows without changes. See [the build config documentation](https://www.nuget.org/packages/Pathoschild.Stardew.ModBuildConfig)
for troubleshooting.

### Compiling a mod for testing
To compile a mod and add it to your game's `Mods` directory:

1. Rebuild the project in [Visual Studio](https://www.visualstudio.com/vs/community/) or [MonoDevelop](http://www.monodevelop.com/).  
   <small>This will compile the code and package it into the mod directory.</small>
2. Launch the project with debugging.  
   <small>This will start the game through SMAPI and attach the Visual Studio debugger.</small>

### Compiling a mod for release
To package a mod for release:

1. Delete the mod's directory in `Mods`.  
   <small>(This ensures the package is clean and has default configuration.)</small>
2. Recompile the mod per the previous section.
3. Create a zip file of the mod's folder in the `Mods` folder. The zip name should include the
   mod name and version (like `ModName-1.0.zip`).
