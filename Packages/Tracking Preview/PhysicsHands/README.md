# Physics Hands

This is a fundamentally different way of interacting with objects compared to the Interaction Engine. Your hands will wrap around objects with your interactions being primarily grounded by physics.

This approach relies heavily on the physics simulation and will work with any rigidbodies and colliders within your scene, without having to add scripts to them. We're going for a physics first design, with extra helpers on the side.

## Requirements
- Unity 2020+
  - This relies heavily on [ArticulationBodies](https://docs.unity3d.com/2020.1/Documentation/ScriptReference/ArticulationBody.html) which are only in 2020+
  - It also makes use of the newer physics solver within Unity.

## Setup
- Setup your scene with hands as you normally would.
- Place the PhysicsProvider in your scene.
- Re-assign the other hands within your scene to have the PhysicsProvider as their Leap Provider.
- Change your physics settings to the following:
  - Solver Type: Temporal Gauss Seidel
- Ensure your rigidbodies have their Collision Detection set to either to Discrete or Continuous Speculative
