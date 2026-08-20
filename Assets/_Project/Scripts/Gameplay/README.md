# Gameplay (Phase 2)

Reserved for run mechanics: level flow, scoring, obstacles, pickups and the gameplay state
extensions to `MoveRush.Core.GameState`.

Rules for anything added here:

- Namespace `MoveRush.Gameplay`.
- Add a `MoveRush.Gameplay.asmdef` that references `MoveRush.Core` only. Gameplay must never
  reference `MoveRush.Managers` directly — resolve `IAudioService`, `ISaveService` and
  `IUIService` through `ServiceLocator`, or subscribe to the relevant `EventBus<T>` channel.
- New states are appended to the `GameState` enum. Existing values keep their numeric value so
  saved data and inspector references stay valid.
