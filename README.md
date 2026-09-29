# Leveling system in Hollow Knight: Silksong

---

**Bonfire-Silksong** is a spiritual successor to Hollow Knight's BonfireMod. It adds a leveling system which allows 
the player to spend rosaries to gain permanent stats, utility and possibly unlock gameplay-altering mechanics.

In addition to leveling menu, there are interfaces for mod settings and a built-in bench fast travel system.  
All of the custom menus can only be accessed during bench visits.

Just like the original Hollow Knight mod, Bonfire-Silksong will naturally 
**break vanilla game balance by making the game generally easier**. Players can of course tune game difficulty to their
liking: while all stat paths are primarily focused on buffs, some of them may even limit the use of basic game 
mechanics.

**=> This mod has an extensive in-game documentation which should be consulted for more details. It will be updated 
between each update and is therefore the best source of most up-to-date information.**
- A separate wiki page/markdown file could be added in future. But for now, please refer to in-game GUI (or the 
*en.loc* localization file which GUI already uses; however this can be hard to read due to long line lengths).


## Table of contents

- [<u>Features</u>](#features)
- [<u>Installation</u>](#installation)
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

#### Future ideas

Mostly depends on what the game looks like after DLC content, but here's some general ideas:

- adjust stat numbers and update existing milestones
- more stat paths & global unlock milestones
- more QoL

And as a final goal: a separate "hardmode" with a couple new gameplay mechanics and enemy/boss reworks. This would be 
build around the existing leveling system, this way it doesn't affect the  non-hardmode experience.

## Installation

- **BepInEx** is the main utility for patching game code with mod's custom logic. 
- support could be extended in the future after a common modding API is introduced i.e. in Hollow Knight this is 
HK modding API + *Lumafly* mod manager -> what will be the Silksong equivalents?

Steps:

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
    - Unzip and move the "Bonfire-Silksong" folder into `Hollow Knight Silksong/BepInEx/plugins` directory.
    Now your path becomes `Hollow Knight Silksong/BepInEx/plugins/Bonfire-Silksong` where this folder should at least
    include two files:
        - `Bonfire-Silksong.dll` (main file)
        - `en.loc` (localization file, contains most of GUI text content)

        There's also the **Bonfire-Silksong.pdb**: this is only for advanced debugging and therefore isn't required to 
        run the mod.


## Compatibility with other mods

**Bonfire-Silksong has quite a bit of complexity in it which can interfere with other gameplay mods. For the best 
experience, you should run it as a standalone gameplay mod.**

If another mod targets any data used in stat values (e.g. needle/spell/tool damage), it will likely break the 
leveling system logic. Same applies for QoL settings: if you add mods which targets an already existing feature 
(e.g. adds enemy health bars), it's best to keep Bonfire-Silksong's own implementation disabled if possible.

The built-in bench fast travel system should however work with other fast travel mods: its logic get applied only 
inside the custom bench menu window. So you are free to ignore it and instead use other similar mods e.g. 
Bonfire Teleport.

**In general:** 
- If two mods modify same areas of game code, there's no guarantee which mod gets to apply its logic first/last. 
It's even possible some parts of code are not executed at all!
- It's up to you to judge/test what works and what doesn't.


## Acknowledgements

While Bonfire-Silksong doesn't directly require other mod dependencies, it has used logic and/or code from following:

- The basis of Leveling system is from Hollow Knight's **BonfireMod** by *ricardosouzag* and 
    **Bonfire** by *TheodoreChristianRadu*.
    - https://github.com/ricardosouzag/BonfireMod (original)
    - https://github.com/TheodoreChristianRadu/Bonfire (fork of BonfireMod)

- Bench fast travel core logic is from **Bonfire Teleport** mod by *kassent*.

    > Author has granted me permission to use their code assets as long as I keep it available free of charge.
    >
    > Furthermore I'm allowed to keep their code when I release this project under MIT License.
    > 
    > I have modified the original code, but it's clearly still uses the general idea of "Bonfite Teleport" i.e. 
    > use bench pins for moving Hornet from scene A to scene B. Modifications include code restructuring and some 
    > additional new features such as 
    > - save the game after teleporting
    > - prevent teleporting if Abyss safety checks are not passed
    > - prevent teleporting in The Slab with Cloakless crest
    
    Bonfire-Silksong and Bonfire Teleport can be used together. There shouldn't be any compatibility issues because 
    both system operate separately: this mod's fast travel is initiated through custom map screen overlay whereas 
    Bonfire Teleport just uses normal map as its access interface.
    
    So if you prefer one fast travel system over another, go for it! 
    
    => [*'Bonfire Teleport'* Nexusmods page](https://www.nexusmods.com/hollowknightsilksong/mods/156)


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