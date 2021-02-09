# InGame-Scriping-SpaceEngineers

This is a collection of utility classes and scripts developed by me (Bazilshep).

## Scripts

You will find short descriptions of the included scripts below.

### `lcd_test`

Demonstrates simple 3D drawing. The script displays a pair of cubes which face a 3d cursor
controlled by a user "flying" in a cockpit (via `IMyShipController.MoveIndicator`).

[Demo](https://imgur.com/fbCxF0Q)

### `control_test` and `controller_test`

The script `control_test` performs waypoint following to commands recieved via the
`IMyIntergridCommunicationSystem`. This is a custom autpilot which provides indepenent
position and orientation control.

Its counterpart `controller_test` issues the commands to follow as
set of waypoints. The waypoints are entered via simple 3D UI.

[Demo](https://imgur.com/a/PceMY7l)

### `radar_test`

WIP.

## ScriptLibs

This is shared project containing reusable script code. Is contains the core of
the implementations of the scripts above.

### `Control.Autopilot`
Code for position and orientation waypoint following.

### `Drawing.Frame3D`
Code for drawing sprites in 3d with depth sorting. Also contains code for drawing
arbitrary triangle sprites, allowign drawing arbitrary triangle meshes!

### `Drawing.Cube`
Code for drawing a 3D cube with simple lighting.

### `Targeting.__Turret`
Code for automatic aiming of turrets. Includes intercept trajectory calculation for constant
acceleration targets. Includes class for Gatling, Interior, and missle turret.

### `TargetTracker.LidarTracker`
Code for tracking multiple targets using multiple `IMyCameraBlock` blocks.
Intelligently uses sensors to maintain target locks and searches for lost targets.

