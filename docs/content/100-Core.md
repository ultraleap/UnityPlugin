# Core {#core}

- **The minimum assets necessary to get Leap hand data into your Unity project.**
  The plugins we package in Core handle all the work of talking to the Leap service running on your platform and providing hand data to your application.

- **A small set of prefabs to get you prototyping with a VR rig and Leap hands right away.**
  Create a new scene and drag in the LMHeadMountedRig prefab to have a working VR rig and Leap hands right away, or drag in the LeapHandController prefab to work with a desktop-mounted Leap Motion Controller.

- **Lightweight utilities for a better Unity development experience, VR or otherwise.**
  These utilities support our Modules and, optionally, your own application: It contains a garbageless LINQ implementation, common data structures, a simple tweening library, and more! Utilizing non-allocating libraries is crucial for VR development, where garbage collection means dropping frames and sickening your users; so our libraries generally eschew implicit allocation in any form.

# Setting up your development environment

Our [developer SDK][devsdk] includes the installer that can get a Leap service running on your platform. You'll need a Leap service to handle the communication between your Leap Motion Controller, your development machine, and your application.

You'll need **Unity 5.5 or later** to use our Core Assets. _However, if you're intending to use the Interaction Engine, it relies on physics features that are only present in **Unity 5.6 or later**._

Of course, you'll also need the Core package and any Modules you'd like to use imported into your Unity project! Refer to our [[Getting Started with Unity Modules | Home]] guide if you're not sure how to do this.

# The prefabs will get you started

The fastest way to get Hands in your Unity environment is to use the **LMHeadMountedRig** prefab (for VR/AR applications) or the **LeapHandController** prefab (for non-VR/AR applications).

If you've gone through the setup process but you aren't seeing hands in your application after adding the LeapHandController prefab or the LMHeadMountedRig prefab to your scene, be sure to check out our [[troubleshooting steps | Home#troubleshooting]].

# The core Leap components

- **LeapServiceProvider** is the class that communicates with the Leap service running on your platform and provides Frame objects containing Leap hands to your application. You can query a LeapServiceProvider for hands, but the recommended workflow is either to use the LeapHandController and HandPool classes (which instantiate hands based on Frame data from a LeapServiceProvider automatically) or subscribe to the LeapServiceProvider's OnUpdateFrame or OnFixedFrame events, which provide Frame objects.
  - You can switch between two test poses at edit-time by using the Edit Time Pose dropdown in the LeapServiceProvider's inspector.
  - LeapServiceProvider is an implementation of **LeapProvider**, which defines the basic interface our modules expect to use to retrieve frame data. This abstraction allows you to create your own LeapProviders, which is useful when testing or developing in a context where Leap Controller hardware isn't immediately. Under most circumstances in which a LeapServiceProvider is used, any implementation of LeapProvider will suffice.

- **LeapHandController** takes Frame data provided from the LeapServiceProvider and creates or updates hand models based on hands that persist from frame-to-frame. It requires a HandPool on the same GameObject to function.

- **HandPool** implements a pool of Hand data objects. It is used by the LeapHandController to re-use data objects as hands enter and exit the frame. In order for the HandPool to function, you must add groups to the Model Pool array -- one for every pair of graphical IHandModels or HandModels you intend to use in your application (Capsule Hands and Rigged Hands, for example, are both implementations of HandModel). Simply increment the Model Pool size on the component and drag in the relevant Left and Right-hand models to the new model group.
  - We also recommend keeping your HandModels in one place, under an organizational transform we label 'Hand Models'. The HandPool provides a slot for this parent object, which it uses as the parent object of duplicate hand models that may need to be spawned. This is necessary in contexts involving more than one user at a time.

The **LeapHandController** prefab contains all three of these components linked together on one GameObject and can get you up and running quickly with Leap hands.

# Developing for VR

![][BasicVRRig]

The **LMHeadMountedRig** prefab is a ready-made VR rig to get you started building a VR application. It consists of the following structure:

- **LMHeadMountedRig** is the root object. This is the Transform you should manipulate if you want to move your player in the VR space (but you should be wary of motion sickness). This object contains the _optional_ VRHeightOffset script by default, which offers a simple way to tune the starting height of your application depending on whether it is running on an Oculus Rift, Vive, or other headset.
  - **CenterEyeAnchor** is the Main Camera. Its local position and rotation are controlled directly by Unity's VR integration; you won't be able to move it manually (for that, you should move its parent, the LMHeadMountedRig). This GameObject also contains the EnableDepthBuffer component and the LeapVRCameraControl component.
    - Childed to the CenterEyeAnchor is the **LeapSpace** object, containing the LeapVRTemporalWarping component. This component manipulates the effective position and rotation of the LeapHandController based on the motion of the VR headset to account for temporal discrepancies between the headset's tracking updates and the controller's tracking updates.
      - In a VR rig, the **LeapHandController** rests beneath the LeapSpace object; internally, Leap hand data is understood to refer to the controller's local space, so the position of this object ultimately determines where Leap hands are spawned and rendered.
  - **HandModels** is a sibling of the CenterEyeAnchor object and is the parent object for all of your HandModels, such as Capsule Hands and Rigged Hands. These objects are automatically enabled and disabled by the LeapHandController based on the tracking state of hands in the controller's view.
  - Any other player-centric objects, such as the Attachment Hands object, can reside as children of the camera rig object.

# Leap hands: The basic set

These hands will get you prototyping right away, and may even serve all the needs of a simple VR application.

## HandModel Implementations

HandModel implementations get automatically pooled by the Hand Pool, but must be added manually as groups in your Hand Pool in order to function. Drop in the prefabs and then add references to the prefabs in your HandPool component to get these hands to render.

### Rigged Hands

![][RiggedHands]

A recent addition to the Core assets, Rigged Hands are the standard hands we use at Leap when building demos or VR content. They are implemented as HandModels, which means they need to be added as an group in your LeapHandController object's HandPool in order to function. If you're interested in using a custom hand mesh with a SkinnedMeshRenderer similarly to the Rigged Hands, you'll want to check out the [[Hands Module | Hands-Module]].

**Note:** For Rigged Hands to look correct, you need to either set your `Quality Settings/Other/Blend Weights` to "4 Bones" (global quality setting) or override the `Skinned Mesh Renderer`s' Quality setting to "4 Bones" (override quality setting for the Rigged Hands). If you don't, the rigged hands will exhibit a strange stretch in the palm.

### Capsule Hands

![][CapsuleHands] 

The Capsule Hands generate a set of spheres and cylinders to render hands using Leap hand data. They render all of the raw data available in a Leap hand in a procedural way. With relatively minor changes to the CapsuleHand. script, you can quickly create a set of hands that match your application's visual style.

# Non-HandModels

### Attachment Hands

![][AttachmentHands]

Attachment Hands aren't usually used to render hands _per se_; rather, you can drag in the AttachmentHands prefab as a sibling of your camera object and use the Transforms that the script auto-generates to easily attach objects to Leap hands or to refer to specific target joints on a hand. To attach an object to a hand, just drag it to be a child of the joint you want to attach it to and align it as desired. Attachment Hands aren't Hand Models, so they don't need to be added to the Hand Pool - to function, they only require an implementation of LeapProvider (e.g. a LeapServiceProvider) to be in your scene.

# Choose your adventure

Now that you know how to get Leap hands rendering in your scene, you may want to know about other functionality available in the Core assets:

- Core also contains useful **[[General Utilities | General-Utilities-in-Core]]** that can make your life easier when performing some common scripting tasks in VR or when prototyping new content.

- Our **[[Modules | https://github.com/leapmotion/UnityModules/wiki#which-modules-are-right-for-you]]** may help if your application requires physical interactions, mobile-friendly rendering, and more!

# FAQ {#core-faq}

**Q: I'm working on a custom experience/headset integration and hand alignment needs to be _totally perfect._ I have control over the head rig that will be used for my experience. How can I make sure the hand aligns with the user's real hands perfectly?**

A: Check your LeapSpace object in the LMHeadMountedRig prefab. We've included an `Allow Manual Device Offset` checkbox in the Advanced section that will allow you to adjust where your application expects the Leap Motion Controller to be relative to the tracked headset. (If you don't see this checkbox, make sure you've upgraded to the latest version of the Core module.)

Because most Leap Motion VR rigs utilize a custom VR developer mount attachment, not all Leap Motion Controllers are mounted in the same place relative to the tracked positions of VR headsets. In order for hands in VR space to align perfectly with hands in the real world, your application needs to know _exactly_ where the Leap Motion Controller is mounted relative to your tracked headset position and orientation. While the default values will usually produce an acceptable experience for VR, in passthrough or mixed-reality situations, a mismatch between the real world Leap position and the VR world Leap position -- even of just a few degrees of tilt, or a centimeter of displacement -- can shift hands too much to produce a plausible tracking experience.

Naturally, this solution isn't viable if you intend for your application to be run on a wide variety of headsets with a Leap Motion Controller attached. Under these circumstances, we recommend you keep the device offsets to their default values (uncheck the checkbox to revert them).

[devsdk]: https://developer.leapmotion.com/get-started/ "Get Started with the Leap Motion SDK"
[BasicVRRig]: http://blog.leapmotion.com/wp-content/uploads/2017/06/BasicVRRig.png

[RiggedHands]: http://blog.leapmotion.com/wp-content/uploads/2017/06/RiggedHands.png
[AttachmentHands]: http://blog.leapmotion.com/wp-content/uploads/2017/06/AttachmentHands.png
[CapsuleHands]: http://blog.leapmotion.com/wp-content/uploads/2017/06/CapsuleHands.png