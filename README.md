# RagdollDemo

A physics-driven third-person character prototype built in Unity 6.

The goal is a character in the spirit of **R.E.P.O.** — one that reacts physically to the world and to impacts, while still answering to the player predictably. The explicit non-goal is the permanently loose, flailing feel of *Human: Fall Flat*.

---

## Getting started

1. Open the project with Unity **6000.3.8f1**.
2. Open `Assets/Scenes/PhysicsCharacterDemo.unity`.
3. Enter Play mode and click inside the Game view — the cursor locks on click.

| Input | Result |
| --- | --- |
| W / S | Move forward / backward along the camera's facing |
| A / D | Strafe left / right without changing facing |
| Mouse X | Turns the character and the camera together |
| Mouse Y | Pitches the camera only; the body stays upright |
| Esc | Releases the cursor and cuts movement input |
| Left-click in Game view | Re-locks the cursor |

Esc is not a pause menu. Physics keeps running; the character simply brakes to a stop.

The scene includes a ramp, 12/18/21 cm steps, and pushable cubes for testing. To trigger a fall on demand, select **Player → Physics Character Balance Controller** during Play and use the component's context menu: `Debug/Force Fall` or `Debug/Force Stagger`.

---

## How the character is built

```
Player          Rigidbody (m = 12) + capsule + 4 scripts     [the controlled body]
  ├ Mascot      visual mesh
  └ Rig         visual skeleton
PhysicsRig      12 physics proxy rigidbodies                 ← a sibling of Player, not a child
Main Camera     over-the-shoulder camera
Plane, Cube×10  ground and test course
```

The physics skeleton is assembled through `ConfigurableJoint.connectedBody`, not through Transform parenting — **13 rigidbodies and 12 joints, all at the same hierarchy level**. The visual skeleton follows the physics proxies in `LateUpdate`.

This detail matters more than it looks: because `PhysicsRig` sits outside `Player`, a call such as `Player.GetComponentsInChildren<ConfigurableJoint>()` finds nothing. New code must take an Inspector reference or walk the joint graph instead.

## Scripts — `Assets/Scripts/Character/`

| File | Responsibility |
| --- | --- |
| `PhysicsCharacterMotor.cs` | Movement, controlled yaw, ground check, step-up solver |
| `PhysicsCharacterBalanceController.cs` | Balance / stagger / fall / recovery state machine |
| `ActiveRagdollPoseDriver.cs` | Resting pose, procedural gait, side-step, get-up pose |
| `PhysicsBoneFollower.cs` | Copies physics rotations onto the visual skeleton |
| `ShoulderCameraController.cs` | Shoulder camera, mouse look, obstruction handling |
| `PhysicsCharacterTelemetry.cs` | Optional CSV diagnostics (see below) |

---

## Telemetry

Telemetry writes one CSV row per physics step to `Diagnostics/`. It is **off by default** and is enabled per developer through the Editor menu:

> **Tools → RagdollDemo → Record Physics Telemetry**

The setting lives in `EditorPrefs`, so turning it on for a tuning session produces no scene diff, and player builds never install the recorder. Adding the component to an object by hand still records regardless of the menu state — a manual add is treated as deliberate intent.

`Diagnostics/` is git-ignored apart from `RecoveryReview/`, which holds the acceptance measurements and pre-change backups referenced by `RECOVERY_SMOOTHING_REVIEW.md`.

---

## Current state

**Working and confirmed by play-testing**

- Camera-relative WASD, controlled yaw, shoulder camera, cursor locking
- Full-body physics proxy chain with visual bone following
- Distance-based procedural gait (2.0 m per full cycle) and A/D side-stepping
- Ramps and 12/18/21 cm steps
- Stop-start whipping resolved by applying movement acceleration to the entire rig
- Fall, get-up, and fall-camera behaviour — **feel approved (2026-09-12)**
- Balance thresholds retuned so that fast turns and minor contacts no longer register as falls

**Next up**

1. Physics hands — `HandPhysics_L/R` proxies and wrist joints (only the visual `Hand_L/R` bones exist today)
2. Object grabbing on top of those hands: reach target, joint-based grip, mass and force limits
3. Bringing every joint drive to a consistent level of "controlledness"
4. Jumping, crouching, running, moving platforms

---

## How we work

- **Project owner:** all code edits plus every Inspector, scene, and physics setting. Owns the call on how movement should feel.
- **Claude:** explains mechanisms, reviews code, analyses telemetry, verifies behaviour, maintains documentation. Does not change code or scene settings unless explicitly asked.

Diagnosis is grounded in the scene file, the scripts, the Unity Console, and the telemetry in `Diagnostics/` — not in guesswork.

---

## Documentation

- **`CLAUDE.md`** — the full technical reference: architecture, joint and mass tables, every tuned value with its source, known limits, telemetry usage. Read it before touching the character.
- **`RECOVERY_SMOOTHING_REVIEW.md`** — measurements and validation boundaries from the 2026-09-08 fall / get-up / camera rework.
- **`Diagnostics/RecoveryReview/`** — acceptance runs, the analysis script, and pre-change backups.
