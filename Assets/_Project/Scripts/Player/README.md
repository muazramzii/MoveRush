# Player (Phase 2)

Reserved for the player character: locomotion, the Input System action asset and its wrapper,
the Cinemachine follow rig and the player-facing animation driver.

Rules for anything added here:

- Namespace `MoveRush.Player`.
- Add a `MoveRush.Player.asmdef` referencing `MoveRush.Core`, `Unity.InputSystem` and
  `Unity.Cinemachine`.
- Input is read through a generated Input System wrapper, never through the legacy `Input` class.
- Movement tuning values belong in a `PlayerMovementConfig` ScriptableObject next to the existing
  configs, not in serialized fields scattered over prefabs.
