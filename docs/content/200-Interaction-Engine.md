# Interaction Engine {#interaction-engine}

The Interaction Engine allows users to work with your VR application by interacting with *physical* or *pseudo-physical* objects. Whether a baseball, a [block][blocks], a virtual trackball, a button on an interface panel, or a hologram with more complex affordances, if there are objects in your application you need your user to be able to **hover near**, **touch**, or **grasp** in some way, the Interaction Engine can do some or all of that work for you.

You can find latest stable Interaction Engine package [on our developer site][devsite].

If you take a look at the examples that come with the package, you'll notice some common patterns that are good to follow when building things with the Interaction Engine.

# The basic components of interaction {#ie-basic-components}

- "Interaction objects" are GameObjects with an attached @ref InteractionBehaviour. They require a Rigidbody and at least one Collider.
- The @ref InteractionManager receives FixedUpdate from Unity and handles all the internal logic that makes interactions possible, including updating hand/controller data and interaction object data. **You need one of these in your scene for interaction objects to function!** A good place for it is right underneath your player's top-level camera rig object (not the camera itself, but its parent).
- Each @ref InteractionController does all the actual _interacting_ with interaction objects, whether by picking them up, touching them, hitting them, or just being near them. This object could be the user's hand by way of the @ref InteractionHand component, or a VR Controller (e.g. Oculus Touch or Vive controller) if it uses the @ref InteractionXRController component. Interaction controllers **should sit beneath the Interaction Manager in the hierarchy** -- see the diagram below.

![](http://blog.leapmotion.com/wp-content/uploads/2017/07/IESetupDiagram.png)

Interaction objects may or may not live inside the player's camera rig depending on whether or not they should move with the player. Interaction controllers, _including_ Interaction Hands, always live underneath the Interaction Manager, and the Interaction Manager should be a sibling of the camera object.

## Physical by Default

When you add an InteractionBehaviour component to an object, a couple of things happen automatically:

- If it didn't have one before, the object will gain a [Rigidbody][rigidbody] component with gravity enabled, making it a physically-simulated object governed by Unity's PhysX engine. If your object doesn't have a [Collider][collider], it will fall through the floor!
- Assuming you have an Interaction Manager with one or more interaction controllers beneath it, you'll be able to pick up, poke, and smack the object with your hands or VR controller.

The first example in the Interaction Engine package showcases the default behavior of a handful of different objects when they first become interaction objects.

[devsite]: https://developer.leapmotion.com/unity "Leap Motion Unity Developer site"
[blocks]: https://www.youtube.com/watch?v=oZ_53T2jBGg&t=1m11s "Leap Motion Blocks demo"
[rigidbody]: https://docs.unity3d.com/ScriptReference/Rigidbody.html
[collider]: https://docs.unity3d.com/ScriptReference/Collider.html

# Working with PhysX objects in Unity {#ie-working-with-physx}

Before scripting behavior with the Interaction Engine, you should know the basics of working with PhysX Rigidbodies in Unity. Most importantly, you should understand Unity's physics scripting execution order:

1. FixedUpdate (user physics logic) **sometimes, but always with PhysX**
2. PhysX updates Rigidbodies and resolves collisions **sometimes, but always with FixedUpdate**
3. Update (user graphics logic) **once every frame**

Source: [this helpful chart from Unity][unity script callback order], via [the execution order page][execution order].

**FixedUpdate** happens just before the physics engine "PhysX" updates and is where user physics logic goes! This is where you should modify the positions, rotations, velocities, and angular velocities of your Rigidbodies to your liking before the physics engine *does physics to them*.

**FixedUpdate may happen 0 or more times per Update.** VR applications usually run at 90 frames per second to avoid sickening the user. Update runs once before the Camera in your scene renders what it sees to the screen or your VR headset. Unity's physics engine has a "fixed timestep" that is configured via Edit->Project Settings->Time. Here at Leap, we build applications with a fixed timestep of `0.0111111` to try and get a FixedUpdate to run once a frame, and this is the setting we recommend. But do note that FixedUpdate **is not** guaranteed to fire before every rendered frame, if your time-per-frame is less that your fixed timestep. Additionally, FixedUpdate may happen two or more times before a rendered frame: this will happen if you spend more than two fixed timesteps' worth of time on any one render frame (i.e. if you "drop a frame" because you tried to do too much work during one Update or FixedUpdate).

Naturally, because the Interaction Engine deals entirely in physics objects, **all interaction object callbacks occur during FixedUpdate**. While we're on the subject of potential gotchas, here are a few more gotchas when working with physics:

- The update order (FixedUpdate, PhysX, Update) implies that if you move physics objects via their Rigidbodies during Update and not during FixedUpdate, the new positions/rotations will not be visible until the *next* update cycle, after the physics engine manipulates objects' Transforms via their Rigidbodies.
- When you move a PhysX object (Rigidbody) via its Transform (`transform.position` or `transform.rotation`) _instead of its Rigidbody_ (`rigidbody.position` or `rigidbody.rotation`), you **force PhysX to immediately do some heavy recalculations internally**, so if you do this a lot to a bunch of physics objects a frame, it could be bad news for your framerate. Generally, we don't recommend doing this! (But sometimes it's necessary.)

# Custom layers for interaction objects {#ie-custom-layers}

Have a custom object layer setup? No problem. Interaction objects need to switch between two layers at runtime: One to exist on when the object **can** collide with your hands (think "contact enabled"), and one to exist on when the object **can't** collide with your hands (When "contact disabled", and also when **grasping** the object).

On a specific Interaction Behaviour under its **Layer Overrides** header, check `Override Interaction Layer` and `Override No Contact Layer` in its inspector to specify custom layers to use for the object when contact is enabled or disabled (e.g. due to being grasped). These layers must follow collision rules with respect to the **contact bone layer**, which is the layer that contains the Colliders that make up the bones in Interaction Hands or Interaction Controllers. (The contact bone layer is usually automatically generated, but you can specify a custom layer to use for Interaction Controllers in the Interaction Manager's inspector.) The rules are as follows:

- The Interaction Layer should have collision enabled with the contact bone layer.
- The No Contact layer should **NOT** have collision enabled with the contact bone layer.
- (Any collision configuration is allowed for these layers with respect to any non-contact-bone layers.)

You can override both or only one of the layers for interaction objects as long as these rules are followed.

# Writing custom behaviors for interaction objects {#ie-custom-interaction-behaviors}

Be sure to take a look at examples 2 through 6 to see how interaction objects can have their behavior fine-tuned to meet the specific needs of your application. The standard workflow for writing custom scripts for interaction objects goes something like this:

- Be sure your object has an @ref InteractionBehaviour component (or an @ref InteractionButton or @ref InteractionSlider component, each of which inherit from @ref InteractionBehaviour).
- Add your custom script to the interaction object and initialize a reference to the @ref InteractionBehaviour component.
```{.cs}
using Leap.Unity.Interaction;
using UnityEngine;

[RequireComponent(typeof(InteractionBehaviour))]
public class CustomInteractionScript : MonoBehaviour {

  private InteractionBehaviour _intObj;

  void Start() {
    _intObj = GetComponent<InteractionBehaviour>();
  }

}
```
- Check out the API documentation (or take advantage of IntelliSense!) for the @ref InteractionBehaviour class to get a sense of what behavior you can control through scripting, or look at the examples below.

## Disabling/enabling interaction types at runtime

Disabling and enabling **hover**, **contact**, or **grasping** at or before runtime is a first-class feature of the Interaction Engine. You have two ways to do this:

### Option 1: Using controller interaction types

The @ref InteractionController class provides the `enableHovering`, `enableContact`, and `enableGrasping` properties. Setting any of these properties to false will immediately fire "End" events for the corresponding interaction type and prevent the corresponding interactions from occurring **from this controller towards any interaction object**.

### Option 2: Using object interaction overrides

The @ref InteractionBehaviour class provides the `ignoreHover`, `ignoreContact`, and `ignoreGrasping` properties. Setting any of these properties to true will immediately fire "End" events for the corresponding interaction type (for this object only) and prevent the corresponding interactions from occurring **from any controller towards this interaction object**.

## Constraining an object's held position and rotation

### Option 1: Use PhysX constraints

The Interaction Engine will obey the constraints you impose on interaction objects whose Rigidbodies you constrain using [Joint][joint] components. If you grasp a *non-kinematic* interaction object that has a Joint attached to it, the object will obey the constraints imposed by that joint.

**If you add or remove an interaction object's Joints at runtime** and your object is graspable, you should call `_intObj.RefreshPositionLockedState()` to have the object check whether any attached Joints or Rigidbody state lock the object's position. Under these circumstances, the object must choose a different grasp orientation solver to give intuitively correct behavior. Check [[ the API documentation on this method | https://developer.leapmotion.com/documentation/unity/class_leap_1_1_unity_1_1_interaction_1_1_interaction_behaviour.html#a33f9f48f2c6375cb926cc94ea2cb6f24 ]] for more details.

### Option 2: Use the OnGraspedMovement callback

When grasped, objects fire their OnGraspedMovement callback **right after the Interaction Engine moves them with the grasping controller**. That means you can take advantage of this callback to **modify the Rigidbody position and/or rotation** just before PhysX performs its physics update. Setting up this callback will look something like this:

```{.cs}

private InteractionBehaviour _intObj;

void Start() {
  _intObj = GetComponent<InteractionBehaviour>();
  _intObj.OnGraspedMovement += onGraspedMovement;
}

private void onGraspedMovement(Vector3 presolvedPos, Quaternion presolvedRot,
                               Vector3 solvedPos,    Quaternion solvedRot,
                               List<InteractionController> graspingControllers) {
  // Project the vector of the motion of the object due to grasping along the world X axis.
  Vector3 movementDueToGrasp = solvedPos - presolvedPos;
  float xAxisMovement = movementDueToGrasp.x;

  // Move the object back to its position before the grasp solve this frame,
  // then add just its movement along the world X axis.
  _intObj.rigidbody.position = presolvedPos;
  _intObj.rigidbody.position += Vector3.right * xAxisMovement;
}

```

## Constraining an interaction object's position and rotation generally

The principles explained above for constraining a **held** interaction object's position and rotation also apply to constraining the interaction object's position and rotation even when it is not held. Of course, Rigidbody Joints will work as expected.

When scripting a custom constraint, however, instead of using the OnGraspedMovement callback, the Interaction Manager provides an OnPostPhysicalUpdate event that fires just after its FixedUpdate, in which it updates interaction controllers and interaction objects. This is would be a good place to apply your physical constraints.

```{.cs}

private InteractionBehaviour _intObj;

void Start() {
  _intObj = GetComponent<InteractionBehaviour>();
  _intObj.manager.OnPostPhysicalUpdate += applyXAxisWallConstraint;
}

private void applyXAxisWallConstraint() {
  // This constraint forces the interaction object to have a positive X coordinate.
  Vector3 objPos = _intObj.rigidbody.position;
  if (objPos.x < 0F) {
    objPos.x = 0F;
    _intObj.rigidbody.position = objPos;

    // Zero out any negative-X velocity when the constraint is applied.
    Vector3 objVel = _intObj.rigidbody.velocity;
    if (objVel.x < 0F) {
      objVel = 0F;
      _intObj.rigidbody.velocity = objVel;
    }
  }
}

```

## Applying forces to an interaction object

If your interaction object is not actively being touched by an Interaction Hand or an Interaction VR Controller, you may apply forces to your Rigidbody using the standard API provided by Unity. However, when an object experiences external forces that press it into the user's controller or the user's hand, **the "soft contact" system provided by the Interaction Engine requires special knowledge of those external forces to properly account for them**. In any gameplay-critical circumstances involving forces of this nature, you should use the Forces API provided by interaction objects:

```{.cs}
_intObj.AddLinearAcceleration(myAccelerationAmount)
_intObj.AddAngularAcceleration(myAngularAccelerationAmount)
```

These accelerations are ultimately applied using the Rigidbody forces API, but are also noted by the "soft contact" system, to prevent the object from nudging its way _through_ any interaction controllers due to repeated application of these forces.

[unity script callback order]: https://docs.unity3d.com/uploads/Main/monobehaviour_flowchart.svg
[execution order]: https://docs.unity3d.com/Manual/ExecutionOrder.html
[rigidbody]: https://docs.unity3d.com/ScriptReference/Rigidbody.html
[collider]: https://docs.unity3d.com/ScriptReference/Collider.html
[joint]: https://docs.unity3d.com/Manual/Joints.html

# Interaction Types In-Depth {#ie-in-depth}

## Hovering

Hover functionality in the Interaction Engine consists of two inter-related subsystems, referred to as 'Hover' and 'Primary Hover' respectively.

### Proximity feedback ("Hover")

Any interaction object within the Hover Activity Radius around an Interaction Controller's hover point will receive the OnHoverBegin, OnHoverStay, and OnHoverEnd callbacks and have its `isHovered` state set to true, as long as both the hovering controller and the interaction object have their hover settings enabled. Interaction objects provide a public getter for getting the closest hovering interaction controller as well. In general, hover information is useful when scripting visual and audio feedback related to proximity.

### Primary Hover

Interaction controllers define one or more "primary hover points," and the closest interaction object (that is currently hovered by an interaction controller) to any of the interaction controller's primary hover points will become the primarily hovered object of that controller. This status can be queried at any time using a controller's `primaryHoveredObject` property or an interaction object's `isPrimaryHovered` property.

Fundamentally, primary hover is the feature that turns unreliable interfaces into reliable ones when _only the primary hovered object_ of a given interaction controller can be depressed or otherwise interacted-with by that controller. This is why the button panel in [[Example 2 (Basic UI) | Getting-Started-(Interaction-Engine)#example-2-basic-ui-in-the-interaction-engine]] will only depress one button per hand at any given time, even if you clumsily throw your whole hand into the panel. The InteractionButton, InteractionToggle, and InteractionSlider classes all implement this primary-hover-only strategy in order to produce more reliable interfaces.

## Contact

Contact in the Interaction Engine consists of two subsystems:
- **Contact Bones**, which are Rigidbodies with a single Collider and a ContactBone component that holds additional contact data for hands and controllers, and
- **Soft Contact**, which activates when Contact Bones get too dislocated from their target positions and rotations -- in other words, when a hand or interaction controller jams itself too far "inside" an interaction object.

### Contact Bones

Interaction controller implementations are responsible for constructing and updating a set of GameObjects with Rigidbodies, Colliders, and ContactBone components, referred to as contact bones. The controller also is responsible for defining the "ideal" position and rotation for a given contact bone at all times. During the FixedUpdate, an interaction controller will set each of its contact bones' velocities and angular velocities such that the contact bone will reach its ideal position and rotation by the _next_ FixedUpdate. These velocities then propagate through the Unity's physics engine (PhysX) update and may the bones may collide against objects in the scene, which will apply forces to them.

Additionally, at the beginning of every FixedUpdate, an interaction controller checks how dislocated a contact bone is from its intended position and rotation. If this dislocation becomes too large, the interaction controller will switch into Soft Contact mode, which effectively disables its contact bones by converting them into [Trigger][triggers] colliders.

### Soft Contact

Soft Contact is essentially an alternative to the standard physical paradigm in physics engines of treating Rigidbodies as, well, perfectly rigid bodies. Instead, relative positions, rotations, velocities, and angular velocities are calculated as the trigger colliders of contact bones pass through the colliders of interaction objects, and custom velocities and angular velocities are applied each frame to any interaction objects that are colliding with the bones of an interaction controller in soft contact mode so that the controller and object will resist motions _deeper into_ the object but freely allow motions _out of_ the object.

If debug drawing is enabled on your Interaction Manager, you can tell when an interaction controller is in Soft Contact mode because its contact bones (by default) will render as white instead of green.

## Grasping

When working with VR controllers, grasping is a pretty basic feature to implement: simply define which button should be used to grab objects, and use the motion of the grasp point to move any grasped object. However, when working with Leap hands, we no longer have the simplicity of dealing in digital buttons. Instead, we've implemented a finely-tuned heuristic for detecting when a user has intended to grasp an interaction object. Whether you're working with VR controllers or hands, the grasping API in the Interaction Engine provides a common interface for constructing logic around grasping, releasing, and throwing.

### Grasped pose & object movement

When an interaction controller picks up an object, the default implementation of all interaction controllers assumes that the intended behavior is for the object to follow the grasp point. Grasp points are explicit for InteractionVRControllers and are implicit for Interaction Hands, but the resulting behavior is the same in either case.

Objects are moved when held under one of two mutually exclusive movement modes: Kinematic, or Nonkinematic. By default, kinematic interaction objects will move kinematically when grasped, and nonkinematic interaction objects will move nonkinematically when grasped. When moving kinematically, an interaction object's rigidbody position and rotation _are set explicitly_, effectively teleporting the object to the new position and rotation. This allows the grasped object to clip through colliders it otherwise would not be able to penetrate. Nonkinematic grasping motions, however, cause an interaction object to instead _receive a velocity and angular velocity_ that will move it to its new target position and rotation on the next physics engine update, which allows the object to collide against objects in the scene before reaching its target grasped position.

When an object is moved because it is being held by a moving controller, a [[special callback | Scripting-Interaction-Objects#option-2-use-the-ongraspedmovement-callback]] is fired right after the object is moved, which you should subscribe to if you wish to modify how the object moves while it is grasped. Alternatively, you can disable the `moveObjectWhenGrasped` setting on interaction objects to prevent their grasped motion entirely (which will no longer cause the callback to fire).

### Throwing

When a grasped object is released, its velocity and angular velocity are controlled by an object whose class implements the IThrowHandler interface. IThrowHandlers receive updates every frame during a grab so that they can accumulate velocity and angular velocity data about the object -- most often, only the latest few frames of data are necessary. When the object is finally released, they get an OnThrow call, which in the default implementation (SlidingWindowThrow) sets the velocity of the object based on a recent historical average of the object's velocity while grasped. In practice, this results in greater intentionality in a user's throws.

However, if you'd like to create a different implementation of a throw, you can implement a new IThrowHandler and set the public `throwHandler` on any interaction object to change how throws behave.

[triggers]: https://docs.unity3d.com/ScriptReference/Collider-isTrigger.html

# FAQ {#interaction-engine-faq}

**Q: Can I translate and rotate my VR rig (player), say, on a moving ship, and still have Interaction Engine user interfaces work?**

A: Yes, we support this via code in the @ref InteractionManager that watches how its own Transform moves in between each FixedUpdate and translating the colliders in the player's Interaction Controllers accordingly. Refer to Example 7 (as of IE 1.1.0) to see a working implementation of this functionality! In general, make sure to:
- Translate and/or rotate the player **during FixedUpdate**, **before the InteractionManager performs its own FixedUpdate**. You can ensure your movement script occurs before the InteractionManager by setting its Script Execution Order to run before Default Time (`Edit/Project Settings/Script Execution Order`). Alternatively, an easy way to receive a callback to execute just before the Interaction Manager's FixedUpdate is to subscribe to the `interactionManager.OnPrePhysicalUpdate` event.
- Make sure the Interaction Manager moves with the player when the player is translated or rotated. The easiest way to do this is to have the Interaction Manager be a child object of your player rig object, e.g., the **LMHeadMountedRig** object.

**Q: Will the Interaction Engine work at arbitrary player scales?**

A: Currently, no. The Interaction Engine works best at "real-world" scale: **1 Unity distance unit = 1 real-world meter.** All of Leap Motion's Unity assets follow this rule, so you're fine if you keep our prefabs at unit scale. If you scale the player too far away from unit scale, certain interactions may stop functioning properly. We are working to support arbitrary interaction scales, but there is no timeline for this feature currently.

**Q: How can I effectively grasp very small objects?**

A: If you need to be able to grasp a very small object and the object's physical colliders don't produce good grasping behaviors, try adding a new primitive collider for the object, such as a SphereCollider, with a larger radius than the object itself, and with isTrigger set to true. As long as the @ref InteractionBehaviour is not ignoring grasping, you will be able to grasp the object by this trigger volume.

However, you don't want a grasping-only trigger collider to be _too_ much larger than the object itself. In general, the larger the grasping volume is around an object, the more likely the user is to accidentally grasp objects when they don't intend to. Additionally, having overlapping grasping-only trigger colliders from multiple objects will prevent the grasp classifier from correctly picking which object to grasp.

**Q: How do I implement two-handed grasping?**

A: You can check a checkbox named 'Allow Multi Grasp' that is located on the interaction object itself to enable two-handed grasp for that object. If you want to know if there are two hands currently grasping an object, you can use the `graspingHands` property of the @ref InteractionBehaviour. This is a set of all hands currently grasping the object, so it will have a count of two if it's currently being grasped by two hands.

## Got a question not answered here?

Head over to [the developer forum][devforum] and post your question! We'll see how we can help.

[devforum]: https://community.leapmotion.com/c/development "Leap Motion Developer Forums"