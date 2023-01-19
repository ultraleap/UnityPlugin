# XR Controller Support

This feature allows you to turn controller data into hand data, in order to use controllers as hands with Ultraleap's tooling. It also includes a Hand<->Controller swapping algorithm, which quickly and robustly swaps between hands and controllers.

## Try out the example scene

To get started, try out the example scene, available from the package manager. The example scene you need to use will differ depending on whether you're using Unity's new [Input System](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.4/manual/index.html), or legacy [Input Manager](https://docs.unity3d.com/2023.1/Documentation/Manual/class-InputManager.html).

| **Input System**     | **Example Scene**                         |
|----------------------|-------------------------------------------|
| New Input System     | XRControllerSupport                       |
| Legacy Input Manager | XRControllerSupport - Legacy Input System |

If you've set up your XR Controllers and Ultraleap tracking correctly, you should be able to swap between Hand input and Controller input in the example scene. When Hand input is active, your current controller positions will be represented by cubes in the scene.

## Setup using prefabs

### 1) Set up Ultraleap Hand Tracking

- Follow the [Getting Started Guide](https://docs.ultraleap.com/unity-api/unity-user-manual/getting-started.html) to install Ultraleap’s Unity Plugin (make sure to import both the ‘Tracking’ and the ‘Tracking Preview’ package)
- Add a the prefabs `Service Provider (XR)` and `CapsuleHands` to your scene. When you hit play, you should now be able to see your tracked hands.

### 2) Choose the correct prefab

The prefab you use will differ depending on whether you're using Unity's new [Input System](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.4/manual/index.html), or legacy [Input Manager](https://docs.unity3d.com/2023.1/Documentation/Manual/class-InputManager.html).

| **Input System**     | **XR Controller Hand Prefab**                                  |
|----------------------|----------------------------------------------------------------|
| New Input System     | `ControllerPostProcessProvider`                                |
| Legacy Input Manager | `ControllerPostProcessProvider Variant - Legacy Input Manager`|

- Drag in the relevant prefab to your scene
- Assign the `Service Provider (XR)` to the `Input Leap Provider` on the `Controller Post Process` Component on the root of the prefab

### 3) Assign your post process provider to your hands

- In each Hand Model in your scene, set the `Leap Provider` to your `ControllerPostProcess` component

Now just hit play! If you've set up your XR Controllers and Ultraleap tracking correctly, you should be able to swap between Hand input and Controller input in the example scene. When Hand input is active, your current controller positions will be represented by cubes in the scene.

## Setup from scratch

### 1) Set up Ultraleap Hand Tracking

- Follow the [Getting Started Guide](https://docs.ultraleap.com/unity-api/unity-user-manual/getting-started.html) to install Ultraleap’s Unity Plugin (make sure to import both the ‘Tracking’ and the ‘Tracking Preview’ package)
- Add a the prefabs `Service Provider (XR)` and `CapsuleHands` to your scene. When you hit play, you should now be able to see your tracked hands.

### 2) Set up your Controller Models

- Add two Gameobjects representing your controllers to your scene, and name them appropriately, e.g. `LeftController` and `RightController`
- Add a [TrackedPoseDriver](https://docs.unity3d.com/2018.3/Documentation/ScriptReference/SpatialTracking.TrackedPoseDriver.html) to each object, setting the `Device` as `Generic XR Controller` and the `Pose Source` according to the appropriate Chirality (e.g. `Left Controller`)
- Add a model of each controller as a child to the Gameobject. Adding as a child object allows you to adjust the object's default offset from the tracked pose driver.
- If you want the models to enable/disable based on whether they are the active input for Ultraleap tooling, add a `ControllerModelEnableDisable` to the parent object of each controller model.

### 3) Set up your Controller Pose Process Provider

- Create an empty `GameObject`, naming it appropriately (e.g. `Controller Post Process Provider`)
- Add the `ControllerPostProcess` component - it should prefill the correct data according to your input system.

### 4) Assign your post process provider to your hands

- In each Hand Model in your scene, set the `Leap Provider` to your `ControllerPostProcess` component

### 5) Add the Hand Controller Swapper Component

- Add the `HandControllerSwapper` component to a GameObject of your choice, and assign your `ControllerPostProcess` component to the `ControllerPostProcess` field

Now just hit play! If you've set up your XR Controllers and Ultraleap tracking correctly, you should be able to swap between Hand input and Controller input in the example scene. When Hand input is active, your current controller positions will be represented by cubes in the scene.

## How do I replace the cubes in the prefabs for my own controller models?

The cube models are driven by `TrackedPoseDrivers`. They don't need to be children of the `Controller Post Process Provider` and can be anywhere in your scene structure. Simply replace the cube with the model of your choice.

## I've swapped input systems and my Controller Hands have stopped working - how do I fix it?

The Controller Post Process should have sensible values & axes set up correctly if using the correct prefab, or if just dragged into the scene. If you have swapped input systems after the post process provider has been in the scene, you can regenerate the default values by using the context menu option "Generate Default Axis" on the Controller Post Process Provider.

If using Unity's legacy Input Manager, you must install the XR Legacy Input Helpers package and use the "Generate Seed Bindings" option in the Assets menu.

## How can I make my own Hand Controller Swapping Algorithm?

`ControllerPostProcess` has a `OnControllerActiveFrame` Action you can subscribe to, which is called every frame a controller is detected. If a controller isn't detected, it defaults back to Leap Hands.

You can set which input `ControllerPostProcess` is using, using `SetInputMethodType` and specifying the `Chirality` (Left/Right), and `InputMethodType` (LeapHand/XRController).

## How can I edit the transform offset of my virtual hands from the controllers?

`ControllerPostProcess` contains Left and Right Hand Inputs, available to edit in the editor. These provide you with the ability to add a positional and rotational offset from the controller transform.

## How can I edit what affect controllers have on my virtual hands?

`ControllerPostProcess` contains Left and Right Hand Inputs, available to edit in the editor. Each finger has a neutral, open pose which it starts in, and a curled pose, which gets affected by the state of the controller Input Axes. You can specify:

- What Input Axes cause the finger to curl
- The rotation of the joints when curled
- Whether to interpolate the joints when transitioning from one state to another, and how fast
