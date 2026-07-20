# Leveling system in Hollow Knight: Silksong

>**If you were looking for a similar mod for _Hollow Knight_, I have an updated version of the original
>Bonfire mod available as [BonfireRelit](https://github.com/j-miet/BonfireRelit) (requires HK modding API)**

---

**Bonfire-Silksong** is a spiritual successor to Hollow Knight's BonfireMod. It adds a leveling system which allows 
the player to spend rosaries to gain permanent raw stats and utility. Leveling GUI is opened when player sits on a 
bench and closes automatically after player stands up.

Just like the original Hollow Knight mod, Bonfire-Silksong will naturally **break vanilla game balance by making everything easier**, but 
also allows players to tune game difficulty and playstyle to their liking.

- In the future, stat formulas will likely get adjusted a bit, but not too much: players should see clear benefits 
even on lower stat investments
- *Hollow Knight: Silksong* is also not in its final state yet, there are multiple free DLCs on the horizon. Need to see first what content gets added and how the progression looks like, especially in the later stages of the game.

For preview images, check the [Nexusmods](#ADD-LINK-HERE) page.

---

The main goals of this mod would be to make the player:

1. Balance between spending rosaries on normal expenses and stat increases
2. Decide on how to allocate stats

    - Stat will only give benefits: you can safely put points into each without any penalty
    - However: committing into 1-3 main stats + optionally putting some on the remaining ones gives you the most 
    benefits. All stats have good scaling so you will become quite strong/overpowered towards the end of the game 
    anyway.
3. Try different playstyles/crests you usually don't care about

    - Stats give you plenty of power, making even generally harder/niche playstyles powerful and more forgivable.
    - Mod also allows you to **unlock all crests** so you can immediately test all of them, and maybe even commit to 
    a specific one for the rest of playthrough. This feature, just the like the leveling system, is accessed by 
    sitting on any bench (thus earliest point for unlocking is just before the first boss).


## Table of contents

- [<u>Features</u>](#features)
- [<u>Leveling System</u>](#leveling-system)
- [<u>Resetting progress</u>](#resetting-progress)
- [<u>Known Issues</u>](#known-issues)
- [<u>Compatibility with other mods</u>](#compatibility-with-other-mods)
- [<u>Installation</u>](#installation)
- [<u>Acknowledgements</u>](#acknowledgements)


## Features
  
All of these are accessed via bench GUI menu when the player sits down on any bench.

Mod consists of 3 core systems:

- **Stat leveling** (see [Leveling System](#leveling-system) section for more details)
- **Quality of life**
  - <u>Shell shard regen</u> (toggle).
  
    Choose between 
    - infinite -> obtaining/using shards fills them to full, obviously breaks Architect Crest balance, or
    - bench-only refill -> fill shards to full, but only at benches. 
      
    Leveling the "Luck" stat also 
    increases enemy shard drop rates if you prefer a more vanilla-like approach for shard management.

  - <u>Enemy hp bars</u> (toggle). 
  
    Just rectangles hovering on enemies, no fancy visuals or numbers.
    Can toggle between
    - single color (enemies = red, bosses = orange) or 
    - progressive (green -> yellow ->  red)

    These can be a bit scuffed: in some scenes there are uninteractable hovering hp bars even when no enemies
    are present. Also some enemies update their health to a different value than they began with
    (e.g. void enemies), but hp bars use only the initial values.

    => For this reason there's also a
    global hotkey `LeftControl + H` to quickly toggle them on/off. 

  - <u>Unlock all crests</u> (permanent unlock) 
  
    For players who want to immediately get access to all crests. Pressing 
this button opens a new smaller window where they can
    - confirm to unlock all crests **permanently** for this save file
    - cancel and not touch current crest progression
    - hide the crest unlock button entirely in case they're not planning to use this feature on this playthrough

    => Selecting either confirm or hide then removes the crest unlock button. It won't be displayed in
  the GUI afterwards.

- **Fast travel between bench location**

    Requirements:
    1. Need to have defeated the ***Clockwork Dancers* boss in Act 2**.
        - this condition will be removed in the future: it heavily incentivizes the player to rush Act 2 and kill the boss, making every single playthrough follow this same path.
    2. You also need to have bought **at least one map AND Bench Pins item from Shakra** to open teleport menu.

    After unlocking
    - player will get a notification pop-up the next time they visit a bench.
    - a new button called "Fast travel menu" appears on top of bench GUI. Clicking this opens the game map, except you 
    are locked out of normal map features in order to avoid any issues.
    - quitting the menu is done by clicking the "Close bench menu" button
        - TODO: allow menu closing by pressing "Inventory" button

    <u>How to use:</u>
    - open fast travel map menu while sitting on a bench
    - click at any available bench location from the list on the left side of screen
    - camera then pans on top of target bench, or if it can't do this e.g. when bench is close to the edge of map, 
    moves as close as possible.
    - then in the bottom-middle, you see the currently selected location name and can click the "Travel" button 
    to initiate travel sequence

## Leveling system

Rosaries now fill a similar role what souls have in Dark Souls games: they are used both for leveling up and as a 
main currency.

There are currently 7 stats in total (6 from the original HK mod + 1 brand new):
- **Strength**
    - needle base damage (works on needle strikes)
- **Dexterity** 
    - needle swing speed (applied to all crest types)
    - crit damage multiplier
- **Intelligence** 
    - silk skill damage 
    - binding cost reduction
- **Artifice** (<u>new in Silksong version</u>)
    - red and blue tool damage
    - passive red tool charge replenishment cooldown 
        - replenishes one charge for each equipped tool -> more red tools = more value
        - replenishment starts at stat lvl 5 to prevent players investing just a single point for big buff
- **Resilience** 
    - add blue masks to hp bar after sitting on bench. 
        - 8 masks is the cap which is reached at stat lvl 40. At this point a single use of plasmium activates the 
        overdose regen effect.
    - chance to block enemy attack (similar to "Magnetite Dice" tool effect)
- **Wisdom** 
    - extra silk from needle hits
        - very strong so increases only every 20 levels (20 = +2, 40 = +3 etc.)
    - passive silk regeneration cooldown
        - just like with Artifice stat, requires at least stat lvl 5 to work
    
    => Wisdom is the weakest early game stat, giving no benefits at all before 5 points.

- **Luck** 
    - needle crit chance
    - increased rosary/shell shard drops from enemies

#### Additional info:
- any tier 3-5 relic returned to Relic Seeker Scrounge gives a free level-up. Free level-ups cost 0 rosaries and will not increase the cost for next level up.

  Following relics thus give free levels
  - choral commandment
  - rune harp
  - arcane egg

  meaning there are 4 + 3 + 1 = 8 free level-ups to be gained during each playthrough.

- levels can be respecced with "Silkeater" item (Hollow Knight's "Rancid Egg" equivalent). 
    - each respec increases this 
cost by one, starting from 1 Silkeater. 
    - respeccing refunds all the rosaries player has spent on levels back to their inventory. Relic levels stay as free level-ups and are immediately available for reallocation.

## Resetting progress

Bonfire-Silksong creates config files individually for each save file. These are stored as

`<your path>/Hollow Knight Silksong/BepInEx/config/Bonfire-Silksong_SaveN.json`

where N is the save slot number.

If you want to reset data for a specific slot, you have to delete corresponding config file manually. Mod will 
automatically create a fresh one after you start a new game in that specific slot.

*There's also another mod config file "hksilksong.mod.bonfire-silksong.cfg"; just ignore it*


## Known issues

- (ONLY A VISUAL BUG) blue health displays wrong visuals when masks are gained from leveling the resiliency stat:

  Lifeblood/Plasmium masks work internally just fine, but the visual is weird: it always displays one 
  extra mask worth of damage, but only on first hit, then works normally. For example:
  - player has 6 blue masks from resiliency stat
  - player takes a hit dealing 2 masks of damage
  - visual goes from 6 -> 3, but internally blue masks go from 6 -> 4 as expected
  - afterwards everything works as expected, only now visual keeps displaying {current_blue_hp - 1}
  - so taking any damage will subtract the same amount, but of course visual will always display that one extra
    offset of damage. So eventually if you reach 1 blue mask, the hp bar displays none but you can still
    absorb a single damage hit before actual masks are removed
  - after all blue health is gone, normal mask hp works just fine, no desync here

  => no idea how to fix this for now. Tried lots of different things, but result is always the same:
  - either have visual offset by -1, or
  - blue mask visuals disappear entirely

  but in both cases internal hp tracking works just fine, making this purely a visual mismatch.

  > TL;DR: blue masks gained by leveling resiliency stat will visually lag a single mask behind: hp bar shows N-1 
  > blue masks, but actual value is N.


## Compatibility with other mods

If another mod targets any data used in stat values (e.g. needle/spell/tool damage), it will likely break the leveling balance. 
This means either less/more scaling per stat level or maybe the value gets overridden entirely, making leveling pointless.

For other features: if you plan to use mods which
- affect shell shard gains/costs
- add their own enemy health bars
- include a crest unlocking feature

then avoid using Bonfire-Silksong's corresponding implementations to avoid conflicts: if they both modify same areas 
of code, there's no guarantee which mod gets to apply its logic first/last.

Built-in bench fast travel system is optional to use. You are free to ignore it or use other similar mods e.g. Bonfire Teleport (to which current system is already based upon; see [Acknowledgements](#acknowledgements))


## Installation

- **BepInEx** is used as the modding API. 
- MelonLoader is not supported at the moment. Might be added at some point, but this requires more work due to having different API.

1. Install BepInEx version 5 if you don't already have it. Version 5.4.23.5 was used during mod development, but any 
5.4.X version should work.
    - downloads are available in
        - [Github](https://github.com/BepInEx/BepInEx/releases)
        - [Nexusmods](https://www.nexusmods.com/hollowknightsilksong/mods/26)
        - [ThunderStore](https://thunderstore.io/c/hollow-knight-silksong/p/silksong_modding/BepInExPack_Silksong/)

    BepInEx first-time install (if already installed, skip to 2.)
    - unzip contents into Silksong path. For example if you have Steam version on Windows this would be something 
    like `C:/Program Files (x86)/Steam/steamapps/common/Hollow Knight Silksong`. There should now be some extra 
    files in the root + a BepInEx directory.
    - run the game once for pre-configuration (not sure if this is 100% required, but it's quick to do anyway)

2. Download **Bonfire-Silksong**:
    - [Github](https://github.com/j-miet/Bonfire-Silksong/releases)
    - NexusMods (coming soon)

3. Install the mod:
    - unzip and move the "Bonfire-Silksong.dll" into `Hollow Knight Silksong/BepInEx/plugins` directory.
    - alternatively, if you like to sort mods by folders, copy the entire mod folder so path 
    becomes instead  `Hollow Knight Silksong/BepInEx/plugins/Bonfire-Silksong`. 
    
        Inside this folder you have again *Bonfire-Silksong.dll* (and possibly *Bonfire-Silksong.pdb* which is just 
        for debugging, it's not necessary for running the mod)


## Acknowledgements

While Bonfire-Silksong doesn't directly require other dependencies, it has used logic and/or code from following mods:

- Hollow Knight's **BonfireMod** by *ricardosouzag* and **Bonfire** by *TheodoreChristianRadu*:
    - https://github.com/ricardosouzag/BonfireMod (original)
    - https://github.com/TheodoreChristianRadu/Bonfire (fork of BonfireMod)
    - my own fork of Bonfire, [BonfireRelit](https://github.com/j-miet/BonfireRelit)
- Fast travel core logic is from **BonfireTeleport** by *kassent*. 

    *Many thanks for giving me the permission to utilize and adapt code into this project. The current fast travel system would most likely not even exists, at least not in its current form!*
    
    Their pin-based teleport system is by far the simplest and most elegant solution to handle fast traveling I've seen in HK/HK: Silksong modding. It's 
    very smooth to use and feels more like a vanilla game feature. Unlike this mod, BonfireTeleport 
    1. adds no external GUI elements added which some users could find immersion-breaking: traveling is initiated 
    from map screen using placing pins
    2. is not locked to bench teleporting only: you can choose between bench-only or just freely jump anywhere at any 
    point (excluding some cases like being in a dream sequence)

    Furthermore, both Bonfire-Silksong and BonfireTeleport can be use together, there shouldn't be any compatibility 
    issues because both system operate separately. So if you prefer one over another, go for it! 
    
    BonfireTeleport's Nexusmods page: https://www.nexusmods.com/hollowknightsilksong/mods/156


#### Modding tools for Hollow Knight

External tools:
- [BepInEx](https://github.com/BepInEx/BepInEx/releases)
- [ILSpy](github.com/icsharpcode/ILSpy)
- [dnSpy](https://github.com/dnSpyEx/dnSpy)
- [FSMExpress](https://github.com/nesrak1/FSMExpress/tree/skong)

Utility mods (requires BepInEx):
- [UnityExplorer](https://github.com/sinai-dev/UnityExplorer)
- [DebugMod](https://thunderstore.io/c/hollow-knight-silksong/p/hk_speedrunning/DebugMod/)
