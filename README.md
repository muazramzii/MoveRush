# MoveRush

Unity 6 LTS · URP · C# · Input System · Cinemachine · TextMeshPro

| Phase | Scope | Status |
| --- | --- | --- |
| 1 — Foundation | Folder structure, clean architecture, state machine, boot flow, JSON save, config assets | Done (this document) |
| 2 — Endless runner core | Three lanes, jump, slide, coins, obstacles, infinite world, pooling, score, difficulty, camera | Done — see [PHASE2.md](PHASE2.md) |

## Phase 1: Foundation

Phase 1 delivers the project skeleton only: folder structure, clean architecture, the
application state machine, the boot flow, a JSON save system and the configuration assets.
No gameplay, no AI, no backend and no UI layout are included by design.

> Phase 2 appended `Gameplay`, `Paused` and `GameOver` to `GameState`, moved the transition table
> into `GameStateTransitions`, and added a fourth required config (`TrackConfig`) plus a `Game`
> scene. Everything below still describes the foundation those changes build on.

---

## First-time setup

1. Open the folder `MoveRush` with Unity 6 LTS (6000.0.x) via Unity Hub. The package manifest
   pulls URP, Input System and Cinemachine on first import. TextMeshPro is not a separate
   package in Unity 6 — it ships inside `com.unity.ugui` 2.0.0, which is already listed.
   If your editor is a different 6000.0 patch and a package version fails to resolve, open
   Package Manager and let it upgrade the entry to the version bundled with your editor.
2. **Create the URP pipeline assets** (a manual step, the URP wizard owns these files):
   `Assets > Create > Rendering > URP Asset (with Universal Renderer)`, save both assets into
   `Assets/_Project/Settings`, then assign the pipeline asset in
   `Project Settings > Graphics > Default Render Pipeline` and in `Project Settings > Quality`.
3. Run `MoveRush > Setup > Run Phase 1 Setup` from the menu bar. This creates the three config
   assets, generates the Bootstrap, Splash and MainMenu scenes, wires the persistent service
   hierarchy and writes the build settings with Bootstrap at index 0.
4. Press Play. `MoveRush > Setup > Always Start From Bootstrap` is on by default, so play mode
   always enters through the bootstrapper regardless of the scene you have open.

Expected console output on a clean run:

```text
[MoveRush] SaveManager initialised. Profile: <persistentDataPath>/moverush.save.json
[MoveRush] AudioManager initialised.
[MoveRush] SceneLoader initialised.
[MoveRush] UIManager initialised.
[MoveRush] GameManager initialised in state Boot.
[MoveRush] Bootstrap: all services are ready.
[MoveRush] GameManager: Boot -> Loading.
[MoveRush] GameManager: Loading -> Splash.
[MoveRush] GameManager: Splash -> Loading.
[MoveRush] GameManager: Loading -> MainMenu.
```

---

## Architecture

```text
Bootstrap  (persistent root, DontDestroyOnLoad, composition root)
├── SceneLoader     ISceneLoader        order 20
├── GameManager     IGameStateService   order 30
├── SaveManager     ISaveService        order  0
├── AudioManager    IAudioService       order 10
└── UIManager       IUIService          order 25
```

**Bootstrap** is the only object marked `DontDestroyOnLoad`. It validates the config assets,
applies platform settings, registers `IConfigProvider`, initialises every `IGameService` in
ascending `InitializationOrder` and then hands control to the state machine. Shutdown runs in
reverse order on application quit.

**Dependency direction** is enforced by assembly definitions:

```text
MoveRush.Core  ←  MoveRush.Managers  ←  MoveRush.Editor
```

Core never references Managers. Managers talk to Core through interfaces resolved from
`ServiceLocator`, so any implementation can be replaced or faked without touching a call site.

**Event-driven flow.** Systems never call each other directly:

- Direct C# events (`IGameStateService.StateChanged`, `ISceneLoader.LoadProgressed`) for the
  services a system already holds.
- `EventBus<T>` for fully decoupled listeners. `EventBusRegistry.ClearAll()` flushes every
  channel on quit so no subscriber survives a domain-reload-free play session.

**State machine.** `GameManager` owns a declarative transition table:

| From     | To                |
| -------- | ----------------- |
| Boot     | Splash, Loading   |
| Splash   | Loading, MainMenu |
| MainMenu | Loading           |
| Loading  | Splash, MainMenu  |

Illegal transitions are rejected and logged instead of throwing, so a mis-wired button cannot
crash a shipped build. Gameplay states are appended in Phase 2 — existing enum values keep their
numeric value.

---

## Save system

JSON, written to `Application.persistentDataPath`. `PlayerPrefs` holds exactly one key
(`MoveRush.SavePath`, the resolved file location) and nothing else.

- `SaveManager` (`ISaveService`) owns the in-memory profile and decides *when* to persist:
  on `Modify(..., saveImmediately: true)`, on the auto-save interval, on application pause and
  on shutdown.
- `JsonSaveRepository` (`ISaveRepository`) owns *where and how* bytes are stored. Writes are
  atomic (temp file, then replace) and the previous file is kept as `.bak` and restored
  automatically when the main file is unreadable.
- `SaveFile` wraps the profile with a schema version, a UTC timestamp and the app version, which
  is what makes future migrations possible instead of a wipe.

Profile contents: `Username`, `Level`, `Xp`, `Coins`, `BestScore`, `Settings`
(master/music/sfx volume, muted, vibration, quality level, language).

Editor helpers: `MoveRush > Save Data > Reveal Save Location / Log Save Contents / Delete Save File`.

Usage:

```csharp
ISaveService save = ServiceLocator.Get<ISaveService>();
save.Modify(data => data.AddCoins(50));
save.Modify(data => data.TrySetBestScore(1200), saveImmediately: true);
```

---

## Configuration

Nothing is hard-coded. Three ScriptableObjects in
`Assets/_Project/ScriptableObjects/Config` drive the whole foundation:

| Asset          | Owns                                                                        |
| -------------- | --------------------------------------------------------------------------- |
| `GameConfig`   | Scene names, splash duration, minimum loading time, frame rate, VSync, quality and language defaults, save file name, auto-save interval, verbose logging |
| `AudioConfig`  | Mixer and exposed parameters, default volumes, sfx pool size, music fade duration, the music and sfx clip bank |
| `PlayerConfig` | Default username, starting level and coins, level cap, XP curve             |

They are published as `IConfigProvider` by the bootstrapper, so every system reads the same data.

---

## Conventions

- One class per file, max 300 lines, XML documentation on every public member.
- Namespaces mirror folders: `MoveRush.Core`, `MoveRush.Core.Config`, `MoveRush.Core.Data`,
  `MoveRush.Core.Events`, `MoveRush.Core.Services`, `MoveRush.Core.Utilities`,
  `MoveRush.Managers`, `MoveRush.Managers.Persistence`, `MoveRush.EditorTools`.
- All logging goes through `MoveRush.Core.Utilities.Log`; informational calls are stripped from
  release builds.
- Serialized fields are private with `[SerializeField]` and exposed through properties.

## Reserved for later phases

`Scripts/Gameplay`, `Scripts/Player`, `Scripts/AI` and `Scripts/Network` are empty on purpose.
Each contains a README stating the namespace, the assembly definition it must declare and the
dependency rules it has to respect.
