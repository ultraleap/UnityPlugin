# Locomotion

This preview package contains experimental locomotion methods - both for teleportation and free locomotion. 

❗❗❗ **This code must be consumed with great warning - it is all due to be completely rewritten to be integrated with XRI.** ❗❗❗

## Teleportation

### Jump Gem Teleportation

To get started with Jump Gem Teleportation, drag the "Jump Gem Teleport With Attachment Hands" prefab into the scene.

Jump Gems are small, pinchable objects which can be used to aim a parabolic ray whilst pinched and released to teleport. Ultraleap recommend attaching a Jump Gem to the user's arm, for quick access.

Jump Gems can be used in either a fixed or free mode.

### Pinch To Teleport

To get started with Pinch To Teleport, drag the "Pinch To Teleport" prefab into the scene.

Pinch To Teleport is a simple teleportation method - point your hand outwards, aim the parabolic ray, and pinch to teleport.

Pinch To Teleport uses `IsFacingObject` to provide a small form of contextual activation - if you're facing a collider in your scene whilst using Pinch To Teleport, it won't activate.

Pinch To Teleport has an optional "Use Rotation" checkbox - if ticked, whilst pinching on a Teleport Anchor, move your hand horizontally to adjust where you'll face when you land.