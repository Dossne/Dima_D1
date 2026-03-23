# Dungeon Cross

Unity 6 2D mobile prototype for a hackathon.

Current direction is a top-down dodge-room game: the player starts at the room entry, avoids moving hazards, and reaches the exit while reading trajectories and danger zones.

## Current State

What is already implemented:
- Runtime bootstrap from `SampleScene` without requiring scene wiring.
- Free 2D player movement instead of old swipe-per-cell grid movement.
- Dungeon room visuals generated in code.
- Moving hazards with visible trajectories.
- Hazard danger radius and optional orbiting blade attack.
- Start screen, HUD, pause menu, game over, level complete, restart flow.
- Best streak and current streak tracking.
- ScriptableObject-based level configs in `Resources/Levels`.
- Custom Unity editor window for level editing.
- Interior wall data, wall rendering, and wall collision for the player.
- Configurable horizontal and vertical hazard path lengths.
- Loop-ready background music support through `MusicManager`.

## Core Gameplay Loop

1. `SceneBootstrap` creates the runtime managers and generated visuals.
2. `StartScreen` blocks gameplay until the player taps to begin.
3. `LevelManager` spawns the current room hazards from `LevelConfig` assets.
4. `PlayerController` moves freely inside room bounds and collides with interior walls.
5. `TrapBase` hazards move in real time and damage the player on overlap.
6. `GameManager` handles HP, level complete, game over, restart, pause, and streaks.

## Main Runtime Scripts

Important scripts under `Assets/Scripts/`:
- `SceneBootstrap.cs`: creates managers/UI/runtime objects at scene start.
- `GameManager.cs`: HP, level flow, pause, streak, restart, win/lose state.
- `GridManager.cs`: room size, world bounds, cell helpers, camera fitting.
- `LevelManager.cs`: loads `LevelConfig` assets and spawns hazards.
- `PlayerController.cs`: free movement, bounds, wall collision, win checks.
- `TrapManager.cs`: active trap registry and collision checks.
- `DungeonVisual.cs`: floor, border walls, interior walls, entry/exit visuals.
- `Audio/MusicManager.cs`: looped BGM loaded from `Resources`.

Hazard scripts:
- `Traps/TrapBase.cs`: shared movement timing, visuals, trajectories, danger logic.
- `Traps/Boulder.cs`: hazard token with horizontal, vertical, or square pathing.
- `Traps/Arrow.cs`: second hazard type using the same path model.

UI scripts:
- `UI/StartScreen.cs`
- `UI/GameUI.cs`
- `UI/PauseMenu.cs`

## Level Data

Primary level data lives in:
- `Assets/Resources/Levels/Level_01.asset`
- `Assets/Resources/Levels/Level_02.asset`
- etc.

Main data types:
- `Data/LevelConfig.cs`
  - `hazards`: list of room hazards
  - `wallCells`: list of interior wall cells
- `Data/RoomHazardConfig.cs`
  - `trapType`
  - `pattern`
  - `startColumn`, `startRow`
  - `minColumn`, `maxColumn` for horizontal path range
  - `minRow`, `maxRow` for vertical path range
  - move interval, direction, danger radius, orbiting blade settings
- `Data/WallCellData.cs`
  - serializable wall cell coordinates

Legacy note:
- `TrapRowConfig` and old trap-row fallback support still exist for compatibility, but the intended current workflow is `LevelConfig + RoomHazardConfig`.

## Level Editor Workflow

Custom editor window:
- Menu: `DungeonCross/Level Editor`

What it does now:
- Lists all `LevelConfig` assets under `Assets/Resources/Levels`.
- Imports built-in fallback levels into editable assets.
- Lets you paint interior walls directly on the room preview.
- Lets you place hazards directly on the grid.
- Lets you select a hazard and edit its settings in the inspector panel.
- Shows hazard trajectory preview and warnings when walls intersect a hazard path.

Current editor model:
- `Wall Layer`: click/drag to paint walls.
- `Hazard Layer`: click to place/select hazards.
- Horizontal hazards can edit `Min Col / Max Col`.
- Vertical hazards can edit `Min Row / Max Row`.
- Square hazards stay fixed at `3x3` for now.

## Audio

Background music is expected at:
- `Assets/Resources/Audio/Music/8BitDungeon.mp3`

Runtime loading path:
- `Resources.Load<AudioClip>("Audio/Music/8BitDungeon")`

Current behavior:
- Music starts from bootstrap/start screen.
- Loops continuously.
- Ignores pause time scale.
- Uses the same `SoundEnabled` PlayerPrefs flag as the pause menu toggle.

## Current Constraints / Known Gaps

Important current limitations:
- The project relies heavily on runtime-generated objects and UI.
- The room is still effectively one-screen and single-room per scene.
- Hazard path editing is numeric, not spline-based.
- Square path size is still fixed at `3x3`.
- Enemy art is still code-generated placeholder token visuals.
- The project has not been fully validated in-editor after every recent script change.

## Safe Next Steps

Good next features to build without changing the whole architecture:
- Better enemy visuals or sprite replacement over procedural tokens.
- Collectables and meta progression hooks.
- Trigger traps such as buttons opening arrow fire from walls.
- Separate `Music` and `SFX` volume/settings instead of one global sound toggle.
- Better hazard editor UX, including drag handles for path bounds.
- More room variety and authored level packs.
- Hazard interaction with walls beyond warnings, if desired later.

## Notes For Another GPT / Engineer

If you continue this project, assume:
- `SampleScene` is mostly a shell; runtime systems are built by `SceneBootstrap`.
- `LevelConfig` assets in `Resources/Levels` are the main source of truth for room content.
- The custom editor is important and should stay aligned with runtime behavior.
- Avoid scene/prefab churn unless absolutely necessary.
- Prefer small, boring script changes over introducing a new framework.
- Verify editor/runtime consistency when changing hazard movement rules.
