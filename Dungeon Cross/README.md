# Dungeon Cross

Unity 6 2D mobile prototype for a hackathon.

Current direction: a top-down dodge-room game where the player starts at the room entry, avoids moving hazards, and reaches the exit while reading trajectories, danger zones, and room layout.

## What Exists Right Now

- Runtime bootstrap from `Assets/Scenes/SampleScene.unity` with minimal scene wiring.
- Free 2D player movement inside a authored room instead of old swipe-per-cell movement.
- Procedural dungeon visuals built in code: floor, perimeter walls, interior walls, entry, exit.
- Real-time hazards with visible paths, danger zones, and optional orbiting blade attacks.
- Start screen, HUD, pause menu, game over, level complete, and final victory flow.
- Streak / best streak tracking.
- ScriptableObject-based level content in `Assets/Resources/Levels/`.
- Custom Unity level editor for hazards and interior walls.
- Background music plus hit / sword / death / victory SFX through a lightweight audio system.

## Core Loop

1. `SceneBootstrap` creates the runtime managers, visuals, UI, and audio.
2. `StartScreen` blocks the run until the player taps to begin.
3. `LevelManager` loads the current `LevelConfig` and spawns room hazards.
4. `PlayerController` moves freely inside room bounds and collides with interior walls.
5. `TrapBase` hazards move in real time and damage the player on overlap.
6. `GameManager` handles HP, pause, level progression, game over, and final run completion.

## Main Runtime Scripts

Under `Assets/Scripts/`:

- [SceneBootstrap.cs](E:/Unity%20Projects/dungeon-cross/Dungeon%20Cross/Assets/Scripts/SceneBootstrap.cs)
  Builds the runtime scene from code.
- [GameManager.cs](E:/Unity%20Projects/dungeon-cross/Dungeon%20Cross/Assets/Scripts/GameManager.cs)
  HP, run state, level progression, pause, game over, final victory.
- [GridManager.cs](E:/Unity%20Projects/dungeon-cross/Dungeon%20Cross/Assets/Scripts/GridManager.cs)
  Room size, world/cell helpers, camera fitting.
- [LevelManager.cs](E:/Unity%20Projects/dungeon-cross/Dungeon%20Cross/Assets/Scripts/LevelManager.cs)
  Loads `LevelConfig` assets and spawns hazards for the current room.
- [PlayerController.cs](E:/Unity%20Projects/dungeon-cross/Dungeon%20Cross/Assets/Scripts/PlayerController.cs)
  Free movement, room bounds, wall collision, win checks.
- [DungeonVisual.cs](E:/Unity%20Projects/dungeon-cross/Dungeon%20Cross/Assets/Scripts/DungeonVisual.cs)
  Floor, perimeter, interior walls, and entry/exit visuals.
- [AudioManager.cs](E:/Unity%20Projects/dungeon-cross/Dungeon%20Cross/Assets/Scripts/Audio/AudioManager.cs)
  Singleton audio layer with separate music and SFX channels.

Hazards:

- [TrapBase.cs](E:/Unity%20Projects/dungeon-cross/Dungeon%20Cross/Assets/Scripts/Traps/TrapBase.cs)
  Shared hazard timing, danger checks, visuals, trajectory drawing, orbiting blade hooks.
- [Boulder.cs](E:/Unity%20Projects/dungeon-cross/Dungeon%20Cross/Assets/Scripts/Traps/Boulder.cs)
  Hazard type with horizontal / vertical / square pathing.
- [Arrow.cs](E:/Unity%20Projects/dungeon-cross/Dungeon%20Cross/Assets/Scripts/Traps/Arrow.cs)
  Second hazard type using the same path model.

UI:

- [StartScreen.cs](E:/Unity%20Projects/dungeon-cross/Dungeon%20Cross/Assets/Scripts/UI/StartScreen.cs)
- [GameUI.cs](E:/Unity%20Projects/dungeon-cross/Dungeon%20Cross/Assets/Scripts/UI/GameUI.cs)
- [PauseMenu.cs](E:/Unity%20Projects/dungeon-cross/Dungeon%20Cross/Assets/Scripts/UI/PauseMenu.cs)

## Level Data

Primary content lives in:

- `Assets/Resources/Levels/Level_01.asset`
- `Assets/Resources/Levels/Level_02.asset`
- etc.

Key data types:

- [LevelConfig.cs](E:/Unity%20Projects/dungeon-cross/Dungeon%20Cross/Assets/Scripts/Data/LevelConfig.cs)
  - `hazards`: list of room hazards
  - `wallCells`: list of interior wall cells
- [RoomHazardConfig.cs](E:/Unity%20Projects/dungeon-cross/Dungeon%20Cross/Assets/Scripts/Data/RoomHazardConfig.cs)
  - `trapType`
  - `pattern`
  - `startColumn`, `startRow`
  - `minColumn`, `maxColumn` for horizontal path bounds
  - `minRow`, `maxRow` for vertical path bounds
  - move interval, direction, danger radius, orbiting blade settings
- [WallCellData.cs](E:/Unity%20Projects/dungeon-cross/Dungeon%20Cross/Assets/Scripts/Data/WallCellData.cs)
  - serializable interior wall cell coordinates

Legacy compatibility still exists for older trap-row data, but the intended workflow is:

`LevelConfig + RoomHazardConfig + WallCellData`

## Level Editor

Custom editor window:

- Menu: `DungeonCross/Level Editor`

Current workflow:

- Lists `LevelConfig` assets from `Assets/Resources/Levels`.
- Can import built-in fallback levels into editable assets.
- Can create new levels with sequential naming (`Level_06`, `Level_07`, etc.).
- `Wall Layer`: paint / erase interior walls directly in the room preview.
- `Hazard Layer`: place hazards on the grid and edit the selected hazard in the inspector area.
- Shows path previews, selected hazard bounds, validation warnings, and wall/path conflicts.
- Supports configurable horizontal and vertical path lengths.

Current editor limitations:

- Hazard paths are still pattern-based, not spline-based.
- Square hazards still use a fixed `3x3` loop.
- Editor UX is intentionally simple and not heavily frameworked.

## Audio

Expected resources:

- `Assets/Resources/Audio/Music/8BitDungeon.mp3`
- `Assets/Resources/Audio/Sfx/Punch03.mp3`
- `Assets/Resources/Audio/Sfx/SwordSlice.mp3`
- `Assets/Resources/Audio/Sfx/freesound_community-pixel-death-66829.mp3`
- `Assets/Resources/Audio/Sfx/astralsynthesizer-11l-victory-1749704552668-358772.mp3`

Current behavior:

- Background music is loaded from `Resources/Audio/Music/8BitDungeon`.
- Music loops and ignores pause time scale.
- Music volume is saved in `PlayerPrefs` under `MusicVolume` and defaults to `0.5`.
- Pause menu has a music volume slider plus the existing sound on/off toggle.
- Generic hit SFX plays on non-lethal damage.
- Sword-specific hit SFX plays for blade hazards.
- Death SFX plays only on game over.
- Victory SFX plays only on final run completion.

## Current Game Flow

- Levels `1-9`: reach exit -> level complete overlay -> continue.
- Level `10`: reach exit -> final victory overlay -> restart prompt.
- Restart returns the run cleanly to level `1`.

## Known Constraints

- Most scene content is generated at runtime.
- The project still uses placeholder procedural visuals for player, hazards, and environment.
- Hazard behavior is intentionally simple and tuned for prototype speed.
- The level editor is useful, but still lightweight and partially manual.
- Not every recent script change has been verified in-editor after each pass.

## Safe Next Steps

Good next features without changing the whole architecture:

- Replace procedural tokens with real sprites or simple animated art.
- Add collectables and meta hooks.
- Add trigger traps such as wall turrets or floor buttons.
- Improve hazard authoring UX further if needed.
- Separate music and SFX toggles/settings more cleanly.
- Add more authored levels and better progression tuning.

## Notes For Another GPT / Engineer

Assume the following before changing things:

- `SampleScene` is mostly a shell; runtime systems come from `SceneBootstrap`.
- `LevelConfig` assets in `Resources/Levels` are the main source of truth.
- Keep changes small, boring, and Unity-safe.
- Avoid scene/prefab churn unless absolutely necessary.
- Keep editor behavior aligned with runtime hazard behavior.
- If you did not run Unity, do not claim in-editor verification.
