# Leveling system in Hollow Knight: Silksong

---

**Bonfire-Silksong** is a spiritual successor to Hollow Knight's BonfireMod, for Hollow Knight: Silksong. It adds a 
leveling system which allows the player to spend rosaries to gain permanent stats. The original system has been greatly
expanded with the addition of stat milestones, making each stat feel unique.

In addition to leveling, interfaces for mod settings and a built-in bench fast travel system are also included.
All of this can be accessed through an extensive custom GUI which stays open during bench visits.

Just like the original BonfireMod, *Bonfire-Silksong* will naturally 
**break vanilla game balance and make the game much easier in general**. However, some of the stat milestones can have optional effects with actual downsides, for example reduce effectiveness of Bind or even disable use of Bind entirely.

#### Help panel

This mod has an extensive in-game documentation which should be consulted for more details.
 - A separate wiki page/markdown file could be added in future. But for now, please refer to the in-game GUI.

The panel can be accessed by clicking the "Help" button on the top-left side on GUI panel.
You don't need to read everything in detail, but it's recommended to skim over the contents: this shouldn't take more 
than 5-10 minutes and gives you a good overview of what Bonfire-Silksong mod has to offer.

And finally, if you want to access the raw GUI text, check the `en.loc` file in the mod folder. This file is work-in-progress: 
currently, it has text contents for entire Help window + most of leveling info. The rest is still hardcoded into `.cs` files, but should eventually get transferred here.


## Table of contents

- [Features](#features)
- [Installation](#installation)
- [Compatibility with other mods](#compatibility-with-other-mods)
- [Acknowledgements](#acknowledgements)


## Features

- An extensive GUI system, accessible while sitting on a bench. GUI is navigated using a cursor, and supports both 
mouse and GamePad users.
    - on GamePad the native game cursor is not available as it's tied to mouse position. A custom cursor object with 
    its own 
    coordinate tracking is used instead. There might be small inconsistencies between button click location and 
    cursor position, but as long as you click closer to the center of each GUI element, it should work.
    - **GamePad controls are simple:**
        - Right Stick to move cursor.
        - Attack/Jump to click.
        - Left/Right bumper to move scrollbars up/down.

- Leveling system: use rosaries to level up stats and gain permanent buffs + other interesting mechanics. Currently 
there are 7 different stats to choose from (6 from original BonfireMod + a new, tool-focused path called *Artifice*).
- Extra settings for additional customization, including some optional gameplay QoL.
- Bench fast travel system allows teleporting between any unlocked bench locations.
    - Most locations are tied to bench pins: if map pin icon is darker/grayed out, it's not unlocked yet.
    - The remaining pinless locations require custom unlock steps e.g. unlocking and visiting your home in Bellhart 
    unlocks the Bellhome bench. These locations can be identified by having a star symbol at the end of their name e.g. "Bellhome ★".


## Installation

Silksong mods can be installed either via a mod manager tool or 
manually. 

#### Mod Managers (recommended)

Silksong has a dedicated open-source mod manager called [Cogfly](https://github.com/Nix-main/Cogfly). It should also
handle installation of BepInEx which is required for mods.

Cogfly has cross-platform support and should work on Linux & MacOS systems.

After installing Cogfly, simple create a profile then search for "Bonfire Silksong".

#### Manual installation

1. Install **BepInEx**. It's essentially the modding API and must be installed to run Silksong mods.

    Version 5.4.23.5 was used during mod development, but any 5.4.X version should work.

    BepInEx can be downloaded from following sources
    - [Github](https://github.com/BepInEx/BepInEx/releases)
    - [Nexusmods](https://www.nexusmods.com/hollowknightsilksong/mods/26)
    - [ThunderStore](https://thunderstore.io/c/hollow-knight-silksong/p/silksong_modding/BepInExPack_Silksong/)

    *BepInEx first-time install*
    - Unzip contents into Silksong install path. For example if you have Steam version on Windows this would be 
    something like `C:/Program Files (x86)/Steam/steamapps/common/Hollow Knight Silksong`. Game directory should now 
    have some additional 
    files such as *winhttp.dll* + a separate *BepInEx* directory .
    - Run the game and quit after reaching the main menu screen: this allows BepInEx to initialize installation, creating 
    the required folders + config files.

2. Download **Bonfire-Silksong** mod:
    - [Github](https://github.com/j-miet/Bonfire-Silksong/releases)
    - NexusMods (coming soon)
    - Thunderstore (coming soon)

3. Install the mod:
    - Move the "Bonfire-Silksong" folder into `<install path>/Hollow Knight Silksong/BepInEx/plugins` directory.
    Your path becomes `<install path>/Hollow Knight Silksong/BepInEx/plugins/Bonfire-Silksong` where this folder 
    should at least contain two files:
        - `Bonfire-Silksong.dll` (main file)
        - `en.loc` (localization file, needed for displaying most of GUI text content)

        There's also the **Bonfire-Silksong.pdb**: this is only for advanced debugging and therefore isn't required to 
        run the mod.

    For example, if have a Windows Steam installation, the full path looks something like this:  
    `C:/Program Files (x86)/Steam/steamapps/Common/Hollow Knight Silksong/plugins/Bonfire-Silksong`


## Compatibility with other mods

**Bonfire-Silksong as a mod is quite complex and will most likely interfere with other gameplay mods. For the best 
experience, <u>you should run it as a standalone gameplay mod</u>.**

If another mod patches the same areas of code (e.g. needle/spell/tool damage), it will likely break the 
leveling system logic. Same applies for QoL settings: if you add mods which targets an already existing feature 
(e.g. adds enemy health bars), it's best to keep Bonfire-Silksong's own implementation disabled if possible.

The built-in bench fast travel system should however work with other fast travel mods: its logic gets applied only 
inside the custom bench menu window. So you are free to ignore use other fast travel mods if you feel like it.

**In general:** 
- If two mods modify same areas of game code, there's no guarantee which mod gets to apply its logic first/last. 
It's even possible some parts of code are not executed at all!
- It's up to you to judge & test what works and what doesn't.


## Acknowledgements

While Bonfire-Silksong doesn't directly require other mod dependencies, it has used logic and/or code of the following mods:

- The basis of Leveling system is from Hollow Knight's **BonfireMod** by *ricardosouzag* and 
    **Bonfire** by *TheodoreChristianRadu*.
    - https://github.com/ricardosouzag/BonfireMod (original)
    - https://github.com/TheodoreChristianRadu/Bonfire (fork of BonfireMod)

- Bench fast travel core logic is from [Bonfire Teleport](https://www.nexusmods.com/hollowknightsilksong/mods/156) mod 
by *kassent*.

    Author of Bonfire Teleport mod has granted me permission to use their mod's code. Code used in Bonfire-Silksong has been modified quite a bit, but it clearly still uses the general idea of "Bonfire Teleport" i.e. 
    use bench pins for moving Hornet from scene A to scene B.
    
    Modifications include restructuring of code + new features, including
    - no free teleporting, limit teleporting from benches only
    - support for custom locations, this is meant for bench locations without pins available
    - saving the game after teleporting
    - prevent teleporting to/from Abyss if safety checks are not passed
    - prevent teleporting from The Slab with Cloakless crest equipped
    - a custom map overlay


#### Modding tools for Hollow Knight

These tools/sources have been essential in developing this mod:

API:
- [BepInEx](https://github.com/BepInEx/BepInEx/releases)

Code inspection:
- [ILSpy](github.com/icsharpcode/ILSpy)
- [dnSpy](https://github.com/dnSpyEx/dnSpy)
- [FSMExpress](https://github.com/nesrak1/FSMExpress/tree/skong)

Utility mods for in-game testing:
- [UnityExplorer](https://github.com/sinai-dev/UnityExplorer)
- [DebugMod](https://thunderstore.io/c/hollow-knight-silksong/p/hk_speedrunning/DebugMod/)

Other:
- [Hollow Knight Wiki](https://hollowknight.wiki/w/Hollow_Knight_Wiki)