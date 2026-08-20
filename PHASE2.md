# MoveRush — Phase 2: Endless Runner Core

A playable vertical slice on top of the Phase 1 foundation: run, three lanes, jump, slide,
coins, obstacles, score, difficulty ramp and game over. Keyboard is the temporary controller.
No AI, no backend, no production UI.

---

## Run it

1. Open the project and run `MoveRush > Setup > Run Phase 1 Setup` (only if you have not already).
2. Run `Window > TextMeshPro > Import TMP Essential Resources`. Without it the HUD has no font.
3. Run `MoveRush > Setup > Run Phase 2 Setup`.
   If Unity asks to restart after the input backend is switched to **Both**, accept and run it
   again — the keyboard provider reads the Input System package and stays silent otherwise.
4. Press Play. The flow is Bootstrap to Splash to MainMenu; press any control to start a run.

**Controls:** `A` / `D` lane, `Space` jump, `S` slide. Arrow keys and `W` mirror them.
After a game over, any control restarts.

---

## Assembly direction

Phase 1 rules are unchanged, with two new assemblies added below Core:

```text
MoveRush.Core  ←  MoveRush.Gameplay     (world, coins, obstacles, score, difficulty, pooling, HUD)
MoveRush.Core  ←  MoveRush.Player       (input, movement, animation, Cinemachine rig)
```

`MoveRush.Gameplay` and `MoveRush.Player` **do not reference each other**. Everything between them
goes through Core:

| Direction | Mechanism |
| --- | --- |
| Player state read by obstacles and coins | `IPlayerService` resolved from `ServiceLocator` |
| Pace read by the character, the score and the spawner | `IDifficultyService` |
| Player intent | `IInputProvider` — keyboard now, pose estimator in Phase 3 |
| Everything else | `EventBus<T>` payloads in `Core/Events` |

Requested event names map to payload types: `OnCoinCollected` → `CoinCollectedEvent`,
`OnPlayerJump` → `PlayerJumpedEvent`, `OnObstacleHit` → `ObstacleHitEvent`,
`OnComboChanged` → `ComboChangedEvent`, `OnGameOver` → `RunEndedEvent`.

`GameState` gained `Gameplay`, `Paused` and `GameOver` — appended, so existing values keep their
numbers. The transition table moved to `Core/GameStateTransitions.cs`.

---

## Modules

### Player — `Assets/_Project/Scripts/Player`

| File | Role |
| --- | --- |
| `PlayerController.cs` | Composes the modules, publishes `IPlayerService`, applies forward motion |
| `LaneMovement.cs` | `SmoothDamp` between the three lanes; target lane snaps, position eases |
| `JumpController.cs` | Hand-integrated gravity, heavier while falling, jump buffering |
| `SlideController.cs` | Timed slide that shrinks the capsule collider |
| `CharacterAnimator.cs` | State machine with parameter-existence guards and a procedural fallback |
| `Input/KeyboardInputProvider.cs` | Temporary `IInputProvider`, lives on the persistent root |
| `Cameras/RunnerCameraController.cs` | Cinemachine FOV, lane tilt and landing impulse |
| `Config/PlayerMovementConfig.cs` | All movement feel values |

Motion is applied to the transform, not the rigidbody. A runner needs a jump arc that is identical
every time, which physics integration cannot promise once obstacle colliders are involved. The
kinematic rigidbody exists only so triggers fire.

### Gameplay — `Assets/_Project/Scripts/Gameplay`

| Area | Files |
| --- | --- |
| Pooling | `ObjectPool<T>`, `PoolService`, `IPoolService`, `IPoolable`, `PooledInstance` |
| World | `WorldGenerator`, `RoadTile`, `TilePool` |
| Coins | `Coin`, `CoinManager`, `CoinPattern` |
| Obstacles | `ObstacleBase`, `Barrier`, `Bus`, `Train`, `LaserGate`, `ObstacleDefinition`, `ObstacleSpawner` |
| Score | `ScoreManager` (`IScoreService`) |
| Difficulty | `DifficultyManager` (`IDifficultyService`) |
| Presentation | `VfxManager`, `PooledEffect`, `GameplayAudioBinder` |
| Run flow | `RunDirector`, `IRunSystem` |
| Temporary UI | `RunHudView`, `RunStarter` |

**Streaming.** A fixed window of tiles is kept around the character: 6 ahead, 1 behind. A tile that
falls out of the window is recycled and reused at the head. Tile length is 20 m, taken from
`TrackConfig`, and the ground mesh stretches to match it. Nothing is ever destroyed.

**Tile ownership.** A tile owns everything spawned onto it and returns it all when recycled, so no
spawner has to remember what it placed. A coin collected mid-run removes itself from its tile first,
which is what stops it being returned twice.

**Fairness.** A row never blocks more lanes than `LaneCount - MinFreeLanes`, rows are never closer
than `MinObstacleGap`, and the first `SafeStartDistance` meters of a run stay empty — measured
against the distance into the run, so a retry gets the same grace period.

**Obstacle rules** live on the obstacle, since only it knows its own: `Barrier` is cleared by a jump,
`LaserGate` by a slide, `Train` and `Bus` only by a lane change. The trigger volumes are sized to
agree with those rules, so geometry and code guard the same outcome twice.

### Score

`Score = Coins × 10 + Distance × 2 + Combo × 50`, weights in `ScoreConfig`. The score is derived
entirely from events; nothing pushes a value in. On a game over the run commits coins, XP and a new
best score to the Phase 1 profile through `ISaveService`.

### Difficulty

`DifficultyConfig` holds the ramp: 0 m → 5, 500 → 6, 1000 → 7, 2000 → 8, 3000 → 9 m/s. The manager
eases towards the step target at `Acceleration` m/s² rather than snapping, and owns both the speed
and the distance so scoring, spawning and framing all read the same numbers. Obstacle density and
the unlocked obstacle set follow the tier: barriers and laser gates from the start, buses at 500 m,
trains at 1000 m.

### Camera

A `CinemachineCamera` with `CinemachineFollow` drives position only; its rotation is authored once
and never touched, which removes the yaw sway that makes a chase camera uncomfortable. On top of it:
FOV opens by up to 8° across the whole speed range, the frame rolls at most 3° into a lane change,
and landing fires an impulse scaled by fall height. Every effect is capped and eased — deliberately,
because a locked chase camera is exactly the setup that causes motion sickness.

---

## Scene assembly

`MoveRush > Setup > Run Phase 2 Setup` generates all of this. The hierarchy it produces:

```text
Game
├── [Systems]              PoolService, RunDirector
│   ├── Difficulty         DifficultyManager      → DifficultyConfig
│   ├── Score              ScoreManager           → ScoreConfig
│   ├── World              TilePool, WorldGenerator
│   ├── Coins              CoinManager            → Coin prefab, 4 patterns, RunnerConfig
│   ├── Obstacles          ObstacleSpawner        → 4 definitions, RunnerConfig
│   └── Presentation       VfxManager, GameplayAudioBinder
├── Player                 (prefab instance at origin)
├── [Camera]
│   ├── Main Camera        Camera + CinemachineBrain
│   └── RunCamera          CinemachineCamera + CinemachineFollow + ImpulseSource + RunnerCameraController
├── Directional Light
└── [HUD]                  Canvas + RunHudView
```

`RunDirector` collects every `IRunSystem` below `[Systems]`, so a new system is added simply by
parenting it there. `PoolService` sits on the rack root because it must exist before any spawner asks
for a pool.

### Inspector reference map

| Component | Field | Value |
| --- | --- | --- |
| `WorldGenerator` | tilePool / coinManager / obstacleSpawner / runnerConfig | the siblings above, `RunnerConfig` |
| `TilePool` | variants | `RoadTile` prefab, weight 1 |
| `CoinManager` | coinPrefab / patterns / runnerConfig | `Coin`, the 4 `CoinPattern` assets, `RunnerConfig` |
| `ObstacleSpawner` | definitions / runnerConfig | the 4 `ObstacleDefinition` assets, `RunnerConfig` |
| `ScoreManager` | scoreConfig | `ScoreConfig` |
| `DifficultyManager` | difficultyConfig | `DifficultyConfig` |
| `VfxManager` | coinEffect / hitEffect / landEffect / runnerConfig | the 3 effect prefabs, `RunnerConfig` |
| `RunnerCameraController` | runCamera / landingImpulse | the components on `RunCamera` |
| `PlayerController` | movementConfig + 4 modules | `PlayerMovementConfig`, siblings on the prefab |
| `SlideController` | bodyCollider / visualRoot | the root `CapsuleCollider`, `Visual` |
| `CharacterAnimator` | animator / visualRoot | leave animator empty until rigged, `Visual` |
| `Bootstrap` (Bootstrap scene) | trackConfig | `TrackConfig` |

### Player prefab structure

```text
Player            tag Player, Rigidbody (kinematic, no gravity), CapsuleCollider (h 2, r 0.4, c 0,1,0)
                  PlayerController, LaneMovement, JumpController, SlideController, CharacterAnimator
└── Visual        pivot at the feet — SlideController scales it, CharacterAnimator rotates it
    └── Body      capsule mesh, no collider
        └── Face  facing marker
```

Scale and rotation are split between the two components on purpose so they never fight over the
transform.

### Building it by hand instead

1. Create the scene and the `[Systems]` rack, add `PoolService` and `RunDirector` to its root.
2. Add one child per system as listed above and assign the fields in the reference map.
3. Drag in the `Player` prefab at the origin.
4. Add a camera with `CinemachineBrain`, a `CinemachineCamera` with `CinemachineFollow`
   (offset `0, 3.4, -6.8`, rotation `11, 0, 0`) targeting the player, plus a `CinemachineImpulseSource`
   and `RunnerCameraController`.
5. Add a Screen Space Overlay canvas with `RunHudView` and its TextMeshPro labels.
6. Add the scene to the build settings after Bootstrap, Splash and MainMenu.
7. In the Bootstrap scene assign `TrackConfig` and add `KeyboardInputProvider` to the persistent root.

---

## Swapping the controller in Phase 3

`IInputProvider` is the only seam the character reads. Replacing keyboard control means writing a
component that implements it, registering it in `ServiceLocator`, and putting it on the bootstrap
root in place of `KeyboardInputProvider`. No gameplay, player or camera code changes.

---

## Known limits of the slice

- Placeholder art only: everything is generated from Unity primitives.
- Audio identifiers on `GameplayAudioBinder` are empty until the Phase 1 audio bank has clips.
- The HUD is functional, not designed. It already implements `IUIScreen`, so the production UI
  replaces it without touching gameplay.
- The character has no Animator controller yet; `CharacterAnimator` drives a procedural lean and
  will pick up real parameters (`Speed`, `Grounded`, `Sliding`, `Dead`, `Jump`) as soon as one exists.
