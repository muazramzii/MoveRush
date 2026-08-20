# Network (Phase 4)

Reserved for the backend layer: authentication, cloud profile sync, leaderboards and remote
config.

Rules for anything added here:

- Namespace `MoveRush.Network`.
- Add a `MoveRush.Network.asmdef` referencing `MoveRush.Core`.
- Cloud persistence arrives as a second `ISaveRepository` implementation, not as a change to
  `SaveManager`. The local JSON repository stays the offline fallback.
- Nothing in this folder is allowed to block the boot flow: network work is asynchronous and the
  game must remain playable offline.
