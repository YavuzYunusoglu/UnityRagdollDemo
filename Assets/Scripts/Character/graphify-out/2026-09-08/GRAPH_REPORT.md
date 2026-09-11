# Graph Report - Character  (2026-09-08)

## Corpus Check
- 6 files · ~3,922 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 124 nodes · 206 edges · 15 communities (8 shown, 5 thin omitted)
- Extraction: 100% EXTRACTED · 0% INFERRED · 0% AMBIGUOUS · INFERRED: 1 edges (avg confidence: 0.8)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `81d1041b`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- PhysicsCharacterMotor
- ActiveRagdollPoseDriver
- PhysicsCharacterTelemetry
- ShoulderCameraController
- PhysicsCharacterBalanceController
- .ApplyDriveScale
- .TryStepUp
- Rigidbody
- .Awake
- RagdollDemo.Character
- BalanceState
- .IsCharacterCollider
- PhysicsCharacterTelemetry

## God Nodes (most connected - your core abstractions)
1. `PhysicsCharacterMotor` - 40 edges
2. `PhysicsCharacterBalanceController` - 36 edges
3. `PhysicsCharacterTelemetry` - 16 edges
4. `ShoulderCameraController` - 16 edges
5. `ActiveRagdollPoseDriver` - 8 edges
6. `PhysicsBoneFollower` - 8 edges
7. `RagdollDemo.Character` - 5 edges
8. `BalanceState` - 5 edges
9. `JointTarget` - 3 edges
10. `JointDriveSnapshot` - 3 edges

## Surprising Connections (you probably didn't know these)
- `PhysicsCharacterBalanceController` --references--> `PhysicsCharacterMotor`  [EXTRACTED]
  PhysicsCharacterBalanceController.cs → PhysicsCharacterMotor.cs
- `PhysicsCharacterMotor` --references--> `ShoulderCameraController`  [EXTRACTED]
  PhysicsCharacterMotor.cs → ShoulderCameraController.cs
- `PhysicsCharacterTelemetry` --references--> `PhysicsCharacterMotor`  [EXTRACTED]
  PhysicsCharacterTelemetry.cs → PhysicsCharacterMotor.cs
- `PhysicsCharacterTelemetry` --references--> `ShoulderCameraController`  [EXTRACTED]
  PhysicsCharacterTelemetry.cs → ShoulderCameraController.cs

## Import Cycles
- None detected.

## Communities (15 total, 5 thin omitted)

### Community 0 - "PhysicsCharacterMotor"
Cohesion: 0.12
Nodes (14): HashSet, InputAction, InputActionAsset, LayerMask, Vector2, PhysicsCharacterMotor, AcceleratesConnectedBodies, IsGrounded (+6 more)

### Community 1 - "ActiveRagdollPoseDriver"
Cohesion: 0.16
Nodes (11): ConfigurableJoint, Rigidbody, Vector3, ActiveRagdollPoseDriver, JointTarget, JointTarget, MonoBehaviour, Quaternion (+3 more)

### Community 2 - "PhysicsCharacterTelemetry"
Cohesion: 0.21
Nodes (8): List, Rigidbody, Vector2, Vector3, PhysicsCharacterTelemetry, RuntimeInitializeOnLoadMethod, StreamWriter, StringBuilder

### Community 3 - "ShoulderCameraController"
Cohesion: 0.23
Nodes (6): LayerMask, RaycastHit, Transform, ShoulderCameraController, HasControl, Yaw

### Community 4 - "PhysicsCharacterBalanceController"
Cohesion: 0.24
Nodes (7): JointDriveSnapshot, Quaternion, Transform, Vector3, PhysicsCharacterBalanceController, CurrentState, RigidbodyConstraints

### Community 7 - "Rigidbody"
Cohesion: 0.33
Nodes (3): CapsuleCollider, ConfigurableJoint, Rigidbody

### Community 9 - ".Awake"
Cohesion: 0.33
Nodes (4): JointDrive, ConfigurableJoint, Rigidbody, JointDriveSnapshot

### Community 11 - "BalanceState"
Cohesion: 0.40
Nodes (5): BalanceState, Balanced, Fallen, Recovering, Staggering

## Knowledge Gaps
- **15 isolated node(s):** `Balanced`, `Staggering`, `Fallen`, `Recovering`, `CurrentState` (+10 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 45 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **5 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `PhysicsCharacterMotor` connect `PhysicsCharacterMotor` to `ActiveRagdollPoseDriver`, `PhysicsCharacterTelemetry`, `ShoulderCameraController`, `PhysicsCharacterBalanceController`, `.TryStepUp`, `Rigidbody`, `.Awake`, `RagdollDemo.Character`, `.IsCharacterCollider`?**
  _High betweenness centrality (0.568) - this node is a cross-community bridge._
- **Why does `PhysicsCharacterBalanceController` connect `PhysicsCharacterBalanceController` to `PhysicsCharacterMotor`, `ActiveRagdollPoseDriver`, `.ApplyDriveScale`, `Rigidbody`, `.TickBalanced`, `.Awake`, `RagdollDemo.Character`, `BalanceState`, `.TickRecovering`?**
  _High betweenness centrality (0.490) - this node is a cross-community bridge._
- **Why does `ShoulderCameraController` connect `ShoulderCameraController` to `PhysicsCharacterMotor`, `ActiveRagdollPoseDriver`, `PhysicsCharacterTelemetry`, `RagdollDemo.Character`?**
  _High betweenness centrality (0.181) - this node is a cross-community bridge._
- **What connects `Balanced`, `Staggering`, `Fallen` to the rest of the system?**
  _15 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `PhysicsCharacterMotor` be split into smaller, more focused modules?**
  _Cohesion score 0.11764705882352941 - nodes in this community are weakly interconnected._