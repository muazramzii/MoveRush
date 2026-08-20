# AI (Phase 3)

Reserved for opponents and bots: steering, decision making and difficulty scaling.

Rules for anything added here:

- Namespace `MoveRush.AI`.
- Add a `MoveRush.AI.asmdef` referencing `MoveRush.Core` and `MoveRush.Gameplay`.
- Behaviour parameters (reaction time, difficulty curves, aggression) live in ScriptableObject
  configs so designers can tune them without a recompile.
