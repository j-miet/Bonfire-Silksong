# Leveling system in Hollow Knight: Silksong

Available on
- [Thunderstore](https://thunderstore.io/c/hollow-knight-silksong/p/Jeooh/Bonfire_Silksong/)
- [Nexus Mods](https://www.nexusmods.com/hollowknightsilksong/mods/1357) 
(images included, these can be viewed without logging in)

---

**Bonfire-Silksong** is a spiritual successor to Hollow Knight's BonfireMod, for Hollow Knight: Silksong. It adds a 
leveling system where the the player can spend rosaries to gain permanent stats. The original system has been greatly
expanded with the addition of stat milestones, making each stat feel even more unique.

In addition to leveling system, interfaces for mod settings and a built-in bench fast travel system are also included.
All of this can be accessed through an extensive custom GUI which stays open during bench visits.

Just like the original BonfireMod, *Bonfire-Silksong* will naturally 
**break vanilla game balance and make the game much easier in general**. Some of the stat milestones however can 
actually have downsides, for example reduce effectiveness of Bind, but they 1. compensate you with another buff 
mechanic and 2. can be always toggled off.

#### Help & mod documentation 

This mod has an extensive in-game documentation which should be consulted for more details.
 - A separate wiki page/file could be added in future. But for now, please refer to the in-game info.

Help panel can be accessed by clicking the "Help" button on the top-left side on GUI panel.
You don't need to read everything in detail, but it's recommended to at least skim over the content: this shouldn't 
take more than 5-10 minutes. 

And finally, if you want to access the raw GUI text, check the `en.loc` file. This file is in a work-in-progress state: 
currently, it has text contents for entire Help window + most of leveling info. The rest is still hardcoded into 
`.cs` files, but should eventually get transferred here.

#### Bugs
Despite spending a lot of time fixing bugs, there are still a few I'm aware of + probably several more I've never 
encountered. Feel free to report them. 

**There's one bug in particular that you as a player should be aware of (this is also mentioned in the Help panel):**  
Sometimes after you use the bench teleport feature, Hornet can get arrive on target scene's bench location and get 
stuck in a falling/hovering animation after standing up. When this happens, you can't pretty much do anything except 
fly around. There are two simple fixes:

- Press `LeftControl + F2` while debug teleport is enabled (it is enabled by default, you can change this in the mod's 
settings). This respawns Hornet into Ruined Chapel bench - the one before first boss. This seems to always 
re-initialize her game state and fix the issue. This does however also update your spawn position to said location.
- Quit to main menu -> reload the save again.

I have not been able to fix this despite it being present ever since teleport system was implement. Problem is, I can 
never reproduce it consistently, it just happens randomly. This could mean its caused by some internal race condition: 
maybe some other part of game code runs before the usual sequence and blocks the update process of Hornet's character 
state. Or maybe it's something very simple and I just fail to see it after spending so much time looking the same code.


## Table of contents

- [Features](#features)
- [Installation](#installation)
- [Compatibility with other mods](#compatibility-with-other-mods)
- [Building from source](#building-from-source)
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

- Leveling system: use rosaries to level up stats and gain permanent buffs + other interesting mechanics. There are 
currently 7 different stats to choose from (6 from original BonfireMod + a new, tool-focused path called *Artifice*).
- Extra settings for additional customization, including some optional gameplay QoL.
- Bench fast travel system allows teleporting between any unlocked bench locations.
    - Most locations are tied to bench pins: if map pin icon is darker/grayed out, it's not unlocked yet.
    - The remaining pinless locations require custom unlock steps e.g. unlocking and visiting your home in Bellhart 
    unlocks the Bellhome bench. These locations can be identified by having a star symbol at the end of their name 
    e.g. "Bellhome ★".

## Installation

**This mod follows the usual BepInEx installation steps.**  
**My experience on Silksong modding is limited to Windows 
only. Linux/Mac users might need to follow a slightly different process.**

Silksong mods can be installed either via a mod manager tool or 
manually.

#### Mod Managers (recommended)

Silksong has a dedicated open-source mod manager called [Cogfly](https://github.com/Nix-main/Cogfly). It should also 
handle BepInEx installation automatically. Manager system integrates with Thunderstore where it finds and downloads
the mod files.

Cogfly claims to have cross-platform support and therefore, should work on Linux & MacOS systems.

After installing Cogfly, simply create a profile then search for "Bonfire Silksong" in the mod list.

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
    have some additional files such as *winhttp.dll* + a separate *BepInEx* directory .
    - Run the game and quit after reaching the main menu screen: this allows BepInEx to initialize installation, 
    creating the required folders + config files.

2. Download **Bonfire-Silksong**.
    - [Thunderstore](https://thunderstore.io/c/hollow-knight-silksong/p/Jeooh/Bonfire_Silksong/)
    - [Nexus Mods](https://www.nexusmods.com/hollowknightsilksong/mods/1357)

3. Install the mod.
    - Move the "Bonfire-Silksong" folder into `<game path>/Hollow Knight Silksong/BepInEx/plugins` directory.
    Your path becomes `<game path>/Hollow Knight Silksong/BepInEx/plugins/Bonfire-Silksong` where this folder 
    should at least contain two files:
        - `Bonfire-Silksong.dll` (main file)
        - `en.loc` (localization file, needed for displaying most of the GUI text content)

        There's also the **Bonfire-Silksong.pdb**: this is only for advanced debugging and therefore isn't required to 
        run the mod.

    For example, if have a Windows Steam installation, the full path looks something like this:  
    `C:/Program Files (x86)/Steam/steamapps/Common/Hollow Knight Silksong/plugins/Bonfire-Silksong`


## Compatibility with other mods

**Bonfire-Silksong as a mod is quite complex and will most likely interfere with other gameplay mods. For the best 
experience, you should run it as a standalone gameplay mod.**

If another mod patches the same areas of code (e.g. needle/spell/tool damage), it will likely break the 
leveling system logic. Same applies for QoL settings: if you add a mod which targets an already existing feature 
(e.g. adds enemy health bars), it's best to keep Bonfire-Silksong's own implementation disabled if possible.

**In general:** 
- If two mods modify same areas of game code, there's no guarantee which mod gets to apply its logic first/last. 
It's even possible some parts of code are not executed at all!
- It's up to you to judge & test what works and what doesn't.


## Building from source

2 different path names are used here:
- `<install path>` = game installation root folder.
- `<mod path>` = mod source code root folder. 

---

If you'd like to customize mod to fit your own needs, you can easily build the `Bonfire-Silksong.dll` again.  
The `<mod path>/Bonfire/Bonfire.csproj` setup file handles this painlessly.

By default, `Bonfire.csproj` uses
- Hollow Knight Silksong's Steam path (*SilksongPath*)
- "Managed" directory for all the game's assemblies i.e. DLL files (*ManagedRefs*)
- BepInEx installation path (*BepInExRefs*)

When you run the C# command `dotnet build`, it automatically builds .dll + .pdb files into  
`<install path>/BepInEx/plugins/Bonfire-Silksong`.
- This will however **not** copy the localization file `en.loc` which must be always found in the same folder as 
`Bonfire-Silksong.dll`. You need to either
    - do this manually by copying it from `<mod path>/Localizations`, or 
    - edit `Bonfire.csproj` and make it auto-copy `en.loc` file to build path as well.

## Acknowledgements

While Bonfire-Silksong doesn't directly require other mod dependencies, it has used logic and/or code from the following 
mods:

- The basis of leveling system is from Hollow Knight's **BonfireMod** by *ricardosouzag* and 
    **Bonfire** by *TheodoreChristianRadu*:
    - https://github.com/ricardosouzag/BonfireMod (original)
    - https://github.com/TheodoreChristianRadu/Bonfire (fork of BonfireMod)

    => Both projects are MIT Licensed.

- Bench fast travel core logic is from [Bonfire Teleport (Nexus Mods)](https://www.nexusmods.com/hollowknightsilksong/mods/156)
by *kassent*.

    *This project contains code from Hollow Knight Silksong's "Bonfire Teleport" mod, made by kassent. They have 
    granted me permission to use my modified version of their project's code. I am also allowed to share this modified 
    code under MIT License.*

    **"Bonfire-Silksong" mod concept itself is not related to "Bonfire Teleport" mod, despite similar naming.**
    - "Bonfire Teleport" is a mod focused on custom fast travel feature, and existed long before Bonfire-Silksong even 
    began its development.
    - "Bonfire-Silksong" is a gameplay mod centered around its leveling system. Its fast travel feature does rely on 
    Bonfire Teleport's code, but includes plenty of own logic as well. Furthermore, this system has been designed as 
    an extra feature: removing it would require very few changes elsewhere in code. Finally, the name "Bonfire-Silksong"
    refers to other mods from Hollow Knight called **BonfireMod** & **Bonfire** 
    (links to both were provided above), which served as its direct inspiration.

    **About the modified code part:** the general idea of "move from scene A to scene B using the bench pin coordinates" 
    is still very much present in the code. More importantly, Bonfire-Silksong uses logic directly taken from 
    Bonfire Teleport mod, which has then been reorganized and/or partially rewritten to better fit Bonfire-Silksong's 
    needs.
    
    Here are some of the features added/changed:
    - no free teleporting, limit teleporting from benches only
    - support for custom locations, this is meant for bench locations without pins available
    - save the game after teleporting
    - prevent teleporting to/from Abyss if safety checks are not passed
    - prevent teleporting from The Slab with Cloakless crest equipped
    - a custom map overlay

    **Finally:** please check Bonfire Teleport out if you already haven't: it's very easy to use and should be 
    compatible with most Silksong mods. In fact you could use it with Bonfire-Silksong, I don't believe there should 
    be any major issue as both mods use slightly different interfaces: 
    - *Bonfire Teleport* uses normal game map + its pin placement function.
    - *Bonfire-Silksong* mod uses a custom map overlay where placing pins is disabled entirely. You must instead use 
    cursor to select bench locations, similar to mods like Benchwarp.

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