# Leveling system in Hollow Knight: Silksong

**Bonfire-Silksong** is a spiritual successor to Hollow Knight's BonfireMod. It adds a leveling system which allows 
the player to spend rosaries to gain permanent raw stats and utility. Bench interface can be accessed when player sits 
on a bench and it then closes automatically after player stands up. 

In addition to leveling menu, there are two additional interfaces for QoL settings and a built-in bench fast travel 
system.

Just like the original Hollow Knight mod, Bonfire-Silksong will naturally 
**break vanilla game balance by making the game generally easier**. Of course players can tune game difficulty and 
playstyle to their liking e.g. by adding self-imposed limits.

- *Hollow Knight: Silksong* is content-wise not in its final state yet, there are still DLCs on the horizon. Need to 
see first what stuff gets added and how it will affect progression, especially in the later stages of the game.
- Then based on these updates, make changes to stats and their scaling formules, add new QoL settings, add more bench 
locations for fast travel etc.  
- And perhaps in the distant future after all DLCs have been released: some kind of optional hardmode could be added 
where stat leveling becomes essential,  otherwise progressing becomes a very difficult task. This idea requires a lot 
more planning, through.

For preview images, check the [Nexusmods](#ADD-LINK-HERE) page.

---

The main goals of this mod would be to make the player:

1. Think and choose how to spend rosaries between normal expenses and stat increases.
2. Decide on how to allocate stats.

    - Stat will only give benefits: you can safely put points into each without any penalty.
    - However, committing into 1-3 main stats + optionally putting some on the remaining ones gives you probably the 
    most benefits in the long run. All stats have good scaling so you will become quite strong/overpowered towards 
    the end of the game anyway.
3. Try different playstyles/crests.

    - Stats give you plenty of power, utility and resources, making even generally harder/niche playstyles more 
    approachable and viable.
    - Mod also has the option to **unlock all crests** so you can immediately juggle between all of them, and maybe 
    even commit to a specific one for the rest of playthrough. This feature, just the like the leveling system, 
    is accessed by sitting on any bench (thus earliest point for unlocking is just before the first boss).


## Table of contents

- [<u>Features</u>](#features)
  - [<u>Leveling System</u>](#leveling-system)
  - [<u>Settings</u>](#settings)
  - [<u>Bench Fast Travel</u>](#bench-fast-travel)
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
- Leveling system: use rosaries to gain permanent primary stats + additional secondary bonuses after reaching 
stat-specific milestones.
- Extra QoL settings for additional customization
- Bench fast travel system whichs allows you to  teleport to any unlocked bench location.
    - Most locations are tied to bench pins: if map pin icon is darker/grayed out, it's not unlocked yet
    - The remaining non-pin locations require custom unlock steps e.g. unlocking and visiting your home in Bellhart 
    unlocks "Bellhome" bench.


The 3 core systems (leveling, settings, fast travel) are explained in more detail below.


### Leveling System

Rosaries now fill a similar role what souls have in Dark Souls games: they are used both for leveling up stats and as a 
main currency.

There are currently 7 stats in total (6 from the original HK mod + a new one for tools called *Artifice*):

<table>
    <tr>
        <th>Stat</th>
        <th><center>Effects</th>
    </tr>
    <tr>
        <td><b>Strength</td>
        <td>
            <ul>
                <li>Needle base damage (therefore it also scales <i>needle strike</i> damage)</li>
            </ul>
        </td>
    </tr>
    <tr>
        <td><b>Dexterity</td>
        <td>
            <ul>
                <li>Needle swing speed (works on all crest types)</li>
                <li>Needle crit damage multiplier</li>
            </ul>
        </td>
    </tr>
    <tr>
        <td><b>Intelligence</td>
        <td>
            <ul>
            <li>Silk Skill damage</li>
            <li>Binding cost reduction</li>
            </ul>
        </td>
    </tr>
    <tr>
        <td><b>Artifice</td>
        <td>
            <ul>
            <li>Red and blue tool damage </li>
            <li>Passive red tool charge replenishment cooldown
                <ul>
                    <li>Replenishes one charge for each equipped tool -> more red tools = more value</li>
                    <li>Replenishment starts at stat lvl 5 to prevent players investing just a single point 
                        for big buff
                    </li>
                </ul>
            </li>
            </ul>
        </td>
    </tr>
    <tr>
        <td><b>Resilience</td>
        <td>
            <ul>
            <li>Add blue masks to hp bar after sitting on bench
                <ul>
                    <li>8 masks is the cap which is reached at stat lvl 40. At this point a single use of 
                    plasmium activates the overdose regen effect.
                    </li>
                </ul>
            </li>
            <li>Chance to block enemy attack (similar to "Magnetite Dice" tool effect)</li>
            </ul>
        </td>
    </tr>
    <tr>
        <td><b>Wisdom</td>
        <td>
        <b># Wisdom is the weakest early game stat, giving no benefits at all before 5 points.
        <h4>Primary</h4>
            <ul>
            <li>Extra silk from needle hits
                <ul>
                    <li>Very strong so 
                    increases only every 20 levels 
                    (stat level < 20 means base value = 1 silk, 20-39 = +2, 40-59 = +3 etc.)
                    </li>
                </ul>
            </li>
            <li>Passive silk regeneration cooldown</li>
                <ul>
                    <li>Just like with Artifice stat, requires at least stat lvl 5 to work</li>
                </ul>
            </li>
            </ul>
        <h4>Secondary</h4>
            <ul>
                <li><b>20 -></b> 
                Wispfire Lantern costs no silk + attack and spawn cooldown reduced by 50%
                (attack: 0.3 -> 0.15, spawn: 4 seconds -> 2 seconds).
                </li>
                <li><b>30 -></b> 
                Wispfire Lantern cooldowns reduces further (attack: 0.1, spawn: 1 second).
                </li>
            </ul>
        <br>
        </td>
    </tr>
    <tr>
        <td><b>Luck</td>
        <td>
            <ul>
            <li>Needle crit chance</li>
            <li>Increased rosary/shell shard drops from enemies</li>
            </ul>
        </td>
    </tr>
</table>

So for example you could try following playstyles + main stat allocations:
- needle/needle strikes (strength, dexterity, luck)
- silk skills (intelligence, wisdom)
- tools (artifice)
- summons: cogflies, Wispfire Lantern, other tools (artifice, wisdom)
- tank (resilience, wisdom, also intelligence for bind cost reduction)

Or just freely mix and test everything, it's your call.


#### Additional info:
- Any tier 3-5 relic returned to Relic Seeker Scrounge gives a free level-up. Free level-ups cost 0 rosaries and will 
not increase the cost of the next level up.

  Following relics thus give free levels
  - Choral Commandment
  - Rune Harp
  - Arcane Egg

  meaning there are 4 + 3 + 1 = 8 free level-ups to be gained during each playthrough.

- Levels can be respecced with "Silkeater" item (Hollow Knight's "Rancid Egg" equivalent). 
    - Each respec increases this 
cost by one, starting from 1 Silkeater. 
    - Respeccing refunds all the rosaries player has spent on levels back to their inventory. Relic levels stay as 
    free level-ups and are immediately available for reallocation.


### Settings
Pressing "Extra Settings" button opens a new window where user can access following quality of life features:

- <u>Gamepad cursor</u> (only available if GamePad device is detected)
    - adjust cursor speed
    - hide mouse cursor: if you have both controller and mouse plugged in and don't need mouse cursor, this hides 
    it in GUI menus

- <u>Enemy hp bars</u>

    Just simple rectangles hovering on enemies, no fancy visuals or numbers.
    Can toggle between
    - *Single color* (enemies = red, bosses = orange) 
    - *Progressive* (green -> yellow ->  red)

    These can be a bit scuffed: in some scenes there are uninteractable hovering hp bars even when no enemies
    are present. Also some enemies update their health during combat
    (e.g. void enemies), but hp bars keep using only the initial values.

    => For this reason there's also a
    global hotkey `LeftControl + H` to quickly toggle them on/off.

- <u>Courier delivery quests</u>
    - remove damage penalties
    - remove timer penalty

- <u>Other</u>
    - Reduce Fleatopia minigame score requirements by 50%

        Just to make minigames less grindy in case you're not in the mood. Visuals will still display the original 
        score requirements, don't mind these.

    - Shell shard regen to full at benches
        
        **[Note]** Leveling the "Luck" stat also 
        increases enemy shard drop rates if you prefer a more vanilla-like approach for shard management.

- <u>Unlock all crests</u> 

    For players who want to immediately access all crests. Pressing 
    this button opens a new smaller window where you can
    - Confirm to unlock all crests **permanently** for this save file
    - Cancel and not touch current crest progression
    - Hide the crest unlock button entirely in case you're not planning to use this feature for this particular 
    playthrough

    => Selecting either confirm or hide then removes the crest unlock button. It won't be displayed in
    the GUI afterwards.

- <u>Reset config file data</u>

    Bonfire-Silksong creates config files individually for each save file. These are stored as

    `<your path>/Hollow Knight Silksong/BepInEx/config/Bonfire-Silksong_SaveN.json`

    where N is the save slot number 1-4, starting from left slot.

    If you want to reset data for new playthroughs, which you should generally do, you would delete the 
    corresponding json file matching your save slot number; mod will automatically make a new one.  
    However, this built-in solution is simpler to use: pressing the button opens a new panel where you can 
    confirm/cancel your choice on resetting data i.e. overriding it with default values. No need for manual file 
    editing/deletion.

### Bench Fast travel

Requirements:
1. Has defeated the ***Bell Beast* boss in Act 1** (primary unlock condition).
2. Has bought **a map AND Bench Pins from Shakra** (to accesss map menu).

#### After unlocking

- Player will get a unlock notification pop-up the next time they visit a bench.
- A new button called "Fast travel" appears on the bottom of bench GUI. Clicking this opens the game map, except you 
are locked out of normal map features to avoid any menu overlap issues.
    - Benches are generally categorized under map areas. Any uncategorized ones can be found in "Other" section at 
    the bottom of the list.
- Quitting the menu is done either by 
    - Clicking the "Close bench menu" GUI button, or
    - Pressing any of the following kb/controller binds: "Inventory", "Map", "Quick Map" or cancel (*Escape* on 
    keyboard)

<u>How to use:</u>
- Click at any available bench location from the list on the left side of screen. 
- Camera then pans on top of target bench, or if it can't do this e.g. when bench is close to the edge of map, 
moves as close as possible.
- Then in the bottom-middle, you see the currently selected location name and can click the "Travel" button 
to initiate travel sequence

*Additional info*
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

- **BepInEx** is used as the modding API. 
- MelonLoader is not supported at the moment. Might be added at some point, but this requires more work due to having 
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

None of these really affect mod functionality, just a bit annoying and unintended behavior. But also easy to fix when
they occur.

- Floating animation bug after using bench fast travel

    Hornet might rarely get stuck in a floating/falling animation when mod forces the destination bench as the
    new spawn location. Probably caused by race conditions between PlayMaker's internal and manually forced states. 
    Haven't been able to replicate this consistently so shouldn't be too much of an issue.

    Fix is simple: save and quit. If this starts to appear more frequently for users, solution is most likely to 
    remove the forced respawn location: teleporting would no longer update spawn location automatically and 
    instead require the player to stand up and sit back down.

    > TL;DR: very rarely after using bench fast travel, Hornet might spawn in floating/falling animation. 
    > Fix is to save & quit then reload back.

- Cursor and button inputs get locked for several seconds after quitting to main menu

    On rare occasions, game temporarily freezes user's cursor movement and keyboard inputs after save & exit. This lasts
    maybe 5 seconds and then all inputs can be used normally again.

    Not sure what causes this as it happens rarely.

    > TL;DR: rarely after save & quit, game controls are locked for a brief moment then works normally again. 
    > Fix is to just wait, this doesn't last long anyway.


## Compatibility with other mods

If another mod targets any data used in stat values (e.g. needle/spell/tool damage), it will likely break the 
leveling system logic. Same applies for QoL settings: if you add mods which target same functionalities, it's best 
to keep Bonfire-Silksong's corresponding implementations disabled. 

In general: if two mods modify same areas 
of code, there's no guarantee which mod gets to apply its logic first/last.

The built-in bench fast travel system should however work with other fast travel mods: its logic get applied only 
inside the custom bench menu window. So you are free to ignore it and instead use other similar mods e.g. 
Bonfire Teleport (to which built-in system is actually already based on; see Acknowledgements section below).


## Acknowledgements

While Bonfire-Silksong doesn't directly require other mod dependencies, it has used logic and/or code from following:

- The basis of Leveling system is from Hollow Knight's **BonfireMod** by *ricardosouzag* and 
    **Bonfire** by *TheodoreChristianRadu*.
    - https://github.com/ricardosouzag/BonfireMod (original)
    - https://github.com/TheodoreChristianRadu/Bonfire (fork of BonfireMod)
    - my fork of Bonfire, [BonfireRelit](https://github.com/j-miet/BonfireRelit)

- Bench fast travel core logic is from **Bonfire Teleport** by *kassent*.
    
    Bonfire Teleport's pin-based teleport system is by far the simplest and most elegant solution to handle fast 
    traveling I've seen in HK/HK: Silksong modding. It's  very smooth to use and feels more like a vanilla game 
    feature. Unlike this Bonfire-Silksong's custom system, Bonfire Teleport
    - Adds no external GUI elements and keeps user interface very vanilla-like: traveling is initiated 
    from map screen by placing pin markers.
    - Is not locked to bench teleporting only: you can choose between bench-only or just freely jump anywhere at any 
    point (excluding some cases like being in a dream sequence). However bear in mind free teleporting can easily 
    softlock game progression if you're not careful - this is the main reason why Bonfire-Silksong prevent it and
    adds further limitations e.g. in Act 3 abyss sections.

    Furthermore, both Bonfire-Silksong and BonfireTeleport can be used together, there shouldn't be any compatibility 
    issues because both system operate separately. So if you prefer one over another, go for it! 
    
    => [*Bonfire Teleport* Nexusmods page](https://www.nexusmods.com/hollowknightsilksong/mods/156)


#### Modding tools for Hollow Knight

These tools have been essential in developing this mod:

Frameworks:
- [BepInEx](https://github.com/BepInEx/BepInEx/releases)

Third-party tools:
- [ILSpy](github.com/icsharpcode/ILSpy)
- [dnSpy](https://github.com/dnSpyEx/dnSpy)
- [FSMExpress](https://github.com/nesrak1/FSMExpress/tree/skong)

Utility mods (requires BepInEx):
- [UnityExplorer](https://github.com/sinai-dev/UnityExplorer)
- [DebugMod](https://thunderstore.io/c/hollow-knight-silksong/p/hk_speedrunning/DebugMod/)