# Leveling system in Hollow Knight: Silksong

---

**Bonfire-Silksong** is a spiritual successor to Hollow Knight's BonfireMod. It adds a leveling system which allows 
the player to spend rosaries to gain permanent stats, utility and possibly unlock gameplay-altering mechanics.

In addition to leveling menu, there are interfaces for custom settings and a built-in bench fast travel system.  
All of mod's custom menus can only be accessed during bench visits.

Just like the original Hollow Knight mod, Bonfire-Silksong will naturally 
**break vanilla game balance by making the game generally easier**. Of course players can tune game difficulty and 
playstyle to their liking e.g. by adding self-imposed limits.

*Hollow Knight: Silksong* is not in its final state yet content-wise as there are still DLCs on the horizon. Need to 
see first what stuff gets added and how it will affect progression, especially in the later stages of the game.
- It's likely that adjustments to stats and their scaling formulas are needed. Also new QoL settings and more bench 
locations for fast travel etc. will be added.
- And perhaps in the distant future after all DLCs have been released: some kind of optional hardmode could be added 
where stat leveling becomes essential or progressing the game could become a very difficult task. This idea requires a 
lot more planning, through.

**=> This mod has an in-game info documentation which should be consulted for more details. It will be updated between 
each update and is therefore the best source of most up-to-date information.**

---

Hopefully his mod can offer even more replayability on top of already extensive base game. For example it should make you:

1. Think and choose how to spend rosaries between normal expenses and stat increases.
2. Experiment with different stat paths.
3. Try out different playstyles/crests.

    - Stats give you plenty of power, utility and resources, making even generally harder/niche playstyles more 
    approachable and viable.
    - Mod also has the option to **unlock crests** (stuff like cursed/cloakless is not included) so you can 
    immediately juggle between all of them, and maybe 
    even commit to a specific one for the rest of playthrough.


## Table of contents

- [<u>Features</u>](#features)
  - [Leveling System](#leveling-system)
  - [Settings](#settings)
  - [Bench Fast Travel](#bench-fast-travel)
- [<u>Installation</u>](#installation)
- [<u>Known Issues</u>](#known-issues)
- [<u>Compatibility with other mods</u>](#compatibility-with-other-mods)
- [<u>Acknowledgements</u>](#acknowledgements)


## Features

- A custom GUI which is accessed by sitting on any bench. GUI is navigated using a cursor and supports both mouse and 
GamePad users.
    - on GamePad the native game cursor is not available as it's tied to mouse. A custom cursor object with it's own 
    coordinate tracking is used instead. Thus there might be small inconsistencies between button click location and 
    cursor position, but as long as you click closer to the center of each GUI element, it should work.
    - **GamePad controls are simple:**
        - Right Stick to move cursor.
        - Attack/Jump to click.
        - Left/Right bumper to move scrollbars up/down.
    
    => Info and Settings windows can be closed by pressing "Spell/Tool" input.
- Leveling system: use rosaries to level up stats and gain permanent benefits + other interesting mechanics.
- Extra settings for additional customization, including some optional gameplay QoL.
- Bench fast travel system whichs allows you to  teleport to any unlocked bench location.
    - Most locations are tied to bench pins: if map pin icon is darker/grayed out, it's not unlocked yet
    - The remaining non-pin locations require custom unlock steps e.g. unlocking and visiting your home in Bellhart 
    unlocks "Bellhome" bench.


The 3 core systems (leveling, settings, fast travel) are explained in more detail below.


### Leveling System

Rosaries now fill a similar role what souls have in Dark Souls games: they are used both for leveling up stats and as a 
main currency.

There are 2 categories:
- global stats/unlocks require certain unlock conditions which is tied total player and/or game progression. These give
quality of live bonuses
- main stats which are used for raising your total level. Each stat gives additional buffs, often centered around a
certain theme like damage source.

#### Main stats
There are currently 7 stat paths in total (6 from the original HK mod + a new one for tools specifically).

Each stat path can have
- primary modifiers: scales with points invested.
- optional secondary modifiers: available after certain milestones.
- milestones: larger bonuses after reaching certain stat threshold, these could alter playstyles a lot.

Primary stats will only give benefits, you can safely put points into each without any penalty.
Milestones can sometimes introduce downsides, but these are always optional and can be toggle them on/off.
    
=> You can never get permanent debuffs from leveling alone.


#### Additional things you should be aware of

- If any milestone mentions crit e.g. "can apply critical hits", this only applies to crits rolled through Luck stat. 
Vanilla crits from Wanderer's Crest or Magnetite Dice don't count! Similarly, Crit Damage from Dexterity applies only 
to Luck crits.
- In vanilla game, needle damage buffs also scale Silk Skills. This is still the case from base game needle upgrades, 
but to any bonus damage from Strength will not benefit Silk Skills further: you need to level Intelligence or find 
global damage multipliers elsewhere.
- In vanilla game, blue masks reset to 0 every time you visit a bench. This mod uses a custom system to prevent this 
for any masks gained from leveling Resilience stat. The system will perform this update after various actions, 
these include: 
    - closing inventory or menu screens
    - applying levels
    - level respeccing
    - after transitioning to new scene via bench fast travel
    
    This system is not perfect: it has been tested quite thoroughly, but sometimes the blue hp updates don't refresh 
    properly and your health var visuals can have 1. missing blue masks and 2. gap between normal and blue masks. If 
    these happen, know that the actual blue hp value should be still updated "but visuals did not. If this happens: the 
    easiest fix is to sit down on a bench again, or if you're already sitting, just open then close any inventory/map 
    screen.
    -> One simple example of failed hp update is when you load into save file, stand up from a bench open inventory as 
    soon as game loads in: game still renders the hp HUD, but is interrupted because "of inventory overlay. If you wait 
    a bit then close inventory, you will get partial or missing blue mask visuals.

    => Know all of this is just to override default blue mask reset behavior. When not on a bench, blue mask 
synchronization should work under game's normal rules with no issues.

- Any tier 3-5 relic returned to Relic Seeker Scrounge gives a free level-up. Free level-ups cost 0 rosaries and will 
not increase the cost of the next level up.

  Following relics thus give free levels
  - Choral Commandment
  - Rune Harp
  - Arcane Egg

  meaning there are 4 + 3 + 1 = 8 free level-ups to be gained during each playthrough.

- Levels can be respecced with "Silkeater" item (Hollow Knight's "Rancid Egg" equivalent). 
    - Each respec increases this cost by one, starting from 1 Silkeater. 
    - Respeccing refunds all the rosaries player has spent on levels back to their inventory. Relic levels stay as 
    free level-ups and are immediately available for reallocation.


### Settings
Pressing "Extra Settings" button opens a new window where user can access features to adjust some of the mod's 
overlay content + apply simple QoL settings. 

=> You can close any Settings-related window

One of the most important features is support for custom Gamepad cursor:
- appears only when Gamepad device is detected
- adds a custom, cursor object which is entirely separate from game cursor. Should work on all GUI menus, although its 
not 100% accurate compared to game cursor. Current texture is also very simple because its generated at runtime: 
white disc with black center.
- can adjust cursor speed
- can hide mouse cursor: if you have both controller and mouse plugged in and don't need mouse cursor, this hides 
    it in bench GUI.


### Bench Fast travel

Requirements:
1. Has defeated the ***Bell Beast* boss in Act 1** (primary unlock condition).
2. Has bought **a map AND Bench Pins from Shakra** (to accesss map menu + pin locations).

#### After unlocking

- A new button called "Fast travel" become available at the bottom of bench GUI. Clicking this opens the game map, 
except you are locked out of normal map features to avoid any menu overlap issues.
    - Benches are generally categorized under map areas. Any uncategorized ones can be found in "Other" section at 
    the bottom of the list.
- Quitting the menu is done either by 
    - Clicking the "Close bench menu" GUI button, or
    - Pressing any of the following kb/controller binds: "Inventory", "Map", "Quick Map", "Spell/Tool" or 
    menu cancel (*Escape* on keyboard)

<u>How to use:</u>
- Click at any available bench location from the list on the left side of screen. 
- Camera then pans on top of target bench, or if it can't do this e.g. when bench is close to the edge of map, 
moves as close as possible.
- Then in the bottom-middle, you see the currently selected location name and can click the "Travel" button 
to initiate travel sequence

#### Additional info
- You can also change bench list style, these will alter colors (basic gray or multicolored) and 
bench names (game scenes or custom names).

- Locked benches e.g. bellshrines (identified by darker map icon) have to be unlocked before you can teleport to them.

- While almost all bench locations are based on map pins, there are also some special benches which don't have one 
available.
    - These are differentiated from pin locations by a star symbol after their name e.g. **"Bellhome ★"**
    - Camera panning feature uses map pins to detect location and move camera. Because these locations don't have 
    such data available, panning is disabled and map camera stays at its previous position after selection.

- In Act 3, there are following abyss limitations to prevent softlocks with story progression: 
    - When you enter abyss the first time, you cannot teleport out until you complete the ascent. You can still travel
    between abyss benches.
    - After ascent, you cannot travel back to abyss before using the fixed diving bell during "Last Dive" quest. 
    After cutscene, all traveling in and out of abyss works normally.


## Installation

- **BepInEx** is the main utility for patching game code with mod's custom logic. 
- MelonLoader is not supported at the moment. Might be added at some point, but this requires more work due to
different API.

1. Install BepInEx version 5 if you don't already have it. Version 5.4.23.5 was used during mod development, but any 
5.4.X version should work.
    - BepInEx can be downloaded from
        - [Github](https://github.com/BepInEx/BepInEx/releases)
        - [Nexusmods](https://www.nexusmods.com/hollowknightsilksong/mods/26)
        - [ThunderStore](https://thunderstore.io/c/hollow-knight-silksong/p/silksong_modding/BepInExPack_Silksong/)

    BepInEx first-time install (if already installed, skip to 2.)
    - Unzip contents into Silksong path. For example if you have Steam version on Windows this would be something 
    like `C:/Program Files (x86)/Steam/steamapps/common/Hollow Knight Silksong`. Game directory should now have some 
    additional 
    files such as *winhttp.dll* + a separate *BepInEx* directory .
    - Run the game and quit after reaching main menu: this allows BepInEx to initialize installation 
    e.g. create config files.

2. Download **Bonfire-Silksong**:
    - [Github](https://github.com/j-miet/Bonfire-Silksong/releases)
    - NexusMods (coming soon)

3. Install the mod:
    - Unzip and move the "Bonfire-Silksong.dll" into `Hollow Knight Silksong/BepInEx/plugins` directory.
    - Alternatively, if you like to sort mods by folders, copy the entire mod folder so path 
    becomes `Hollow Knight Silksong/BepInEx/plugins/Bonfire-Silksong`. 
    
        Inside this folder you have again *Bonfire-Silksong.dll* (and possibly *Bonfire-Silksong.pdb* which is just 
        for advanced debugging, it's not necessary for running the mod)


## Known issues

- Floating animation bug after using bench fast travel.

    Hornet might rarely get stuck in a floating/falling animation when mod forces the destination bench as the
    new spawn location. Probably caused by race conditions between PlayMaker's internal and manually forced states. 
    Haven't been able to replicate this consistently so shouldn't be too much of an issue.

    Fix is simple: save and quit. If this starts to appear more frequently for users, solution is most likely to 
    remove the forced respawn location: teleporting would no longer update spawn location automatically and 
    instead require the player to stand up and sit back down.

    > TL;DR: very rarely after using bench fast travel, Hornet might spawn in floating/falling animation. 
    > Fix is to save & quit then reload back.


## Compatibility with other mods

Bonfire-Silksong has quite a bit of complex gameplay logic in it and therefore works best as a standalone main mod.

If another mod targets any data used in stat values (e.g. needle/spell/tool damage), it will likely break the 
leveling system logic. Same applies for QoL settings: if you add mods which target same functionalities, it's best 
to keep Bonfire-Silksong's corresponding implementations disabled. 

The built-in bench fast travel system should however work with other fast travel mods: its logic get applied only 
inside the custom bench menu window. So you are free to ignore it and instead use other similar mods e.g. 
Bonfire Teleport (to which built-in system is actually already based on; see Acknowledgements section below).

**In general:** 
- If two mods modify same areas 
of code, there's no guarantee which mod gets to apply its logic first/last.
- It's up to you to judge/test what works and what doesn't.


## Acknowledgements

While Bonfire-Silksong doesn't directly require other mod dependencies, it has used logic and/or code from following:

- The basis of Leveling system is from Hollow Knight's **BonfireMod** by *ricardosouzag* and 
    **Bonfire** by *TheodoreChristianRadu*.
    - https://github.com/ricardosouzag/BonfireMod (original)
    - https://github.com/TheodoreChristianRadu/Bonfire (fork of BonfireMod)
    - my fork of Bonfire, [BonfireRelit](https://github.com/j-miet/BonfireRelit)

- Bench fast travel core logic is from **Bonfire Teleport** by *kassent*.
 
    Bonfire-Silksong and Bonfire Teleport can be used together. There shouldn't be any compatibility issues because 
    both system operate separately: this mod's fast travel is initiated through custom map screen overlay whereas 
    Bonfire Teleport just uses vanilla map as its interface.
    
    So if you prefer one fast travel system over another, go for it! 
    
    => [*Bonfire Teleport* Nexusmods page](https://www.nexusmods.com/hollowknightsilksong/mods/156)


#### Modding tools for Hollow Knight

These tools/sources have been essential in developing this mod:

Core:
- [BepInEx](https://github.com/BepInEx/BepInEx/releases)

Third-party tools to inspect game code:
- [ILSpy](github.com/icsharpcode/ILSpy)
- [dnSpy](https://github.com/dnSpyEx/dnSpy)
- [FSMExpress](https://github.com/nesrak1/FSMExpress/tree/skong)

Utility mods:
- [UnityExplorer](https://github.com/sinai-dev/UnityExplorer)
- [DebugMod](https://thunderstore.io/c/hollow-knight-silksong/p/hk_speedrunning/DebugMod/)

Other:
- [Hollow Knight Wiki](https://hollowknight.wiki/w/Hollow_Knight_Wiki)