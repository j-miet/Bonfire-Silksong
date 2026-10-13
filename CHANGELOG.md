# Bonfire-Silksong changelog

## v1.0.2

#### Bug fixes
Resilience:
- "Hollow" state should no longer prevent the use of "Bind" input outside its healing action. This would previously 
make binding/learning new abilities (silk spear, dash etc.) impossible.
- The flat damage bonus from second milestone in "Hollow" state will now properly check the milestone requirement.
- Resilience blue hp value is now updated if player dies and respawns in a non-bench location e.g. you unlock "silk spear" skill -> die -> respawn in front of silk spear unlock location.

Other
- fix a localization typo in GUI settings
- code: added a few more null checks to prevent Unity log error during mod load sequence. 

## v1.0.1
- Moved settings and bench teleport GUI text into `en.loc` file. Also made some minor adjustment to localization contents.
- Source code now publicly available on Github under MIT License; you can find it [here](https://github.com/j-miet/Bonfire-Silksong).

## v1.0.0
- Full release
- Main features:
    - leveling system: use rosaries to gain permanent power-ups and access new gameplay mechanics.
    - settings panel: simple enemy health bars and other quality of life, all optional to use.
    - bench fast travel overlay, rendered on top of game map.
    - an extensive GUI, fully compatible for both mouse & gamepad users.