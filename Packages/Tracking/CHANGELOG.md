# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

[docs-website]: https://docs.ultraleap.com/unity-api/ "Ultraleap Docs"

## [6.13.0] - 24/01/24

### Tracking Client versions
- Windows 	v5.17.1
- MacOS 	v5.17.1
- Android 	v5.17.1

### Added
- Physical Hands. This introduces a new way of interacting with object in the virtual world using your hands and unitys physics engine.

### Changed
- Removed Physics Hands from the preview package as Physics Hands has replaced it.


### Known issues
- Repeatedly opening scenes can cause memory use increase
- The OpenXR Leap Provider does not currently support the `Confidence` hand property (and will return fixed values)
- After using Ultraleap OpenXR in Unity Editor, the tracking mode of device 0 will be set to HMD until the Unity Editor session ends. This can stop the testing of non-XR scenes until the Unity Editor is re-opened

## [6.13.0] - 24/11/23

### Tracking Client versions
- Windows 	v5.17.1
- MacOS 	v5.17.1
- Android 	v5.17.1

### Added
- (Hands Recorder Preview) A new Hand Recorder to record animation clips of hands and additional objects to produce re-playable actions (e.g for tutorials)
- (Pose Detector) Add a new rule type to match rotation of a joint to a target
- (XR Hands) Added XRHandsLeapProvider to convert the current XRHandsSubsystem data to a LeapProvider for use with the Ultraleap Unity Plugin features
- Added the ability to suppress the Android build warnings
- Help Menu that links out to docs, a place to report bugs & places to get support

### Changed
- (Preview Teleportation) Lightweight Pinch Detector's finger detection can be configured
- (Obsolete) Some old unused LeapC APIs have been marked as Obsolete (config, IsSmudged, IsLightingBad)
- Changed from using obsolete FindObjectOfType to using newer implementations
- Ultraleap settings are now in the project settings window, under "Ultraleap"

### Fixed
- (Hand Binder) Hands begin at large/small scale and slowly scale to normal size
- (Pinch to Paint Example) Painting sound does not play if pinch began out of tracking range
- LeapXRServiceProvider ensures default device offset mode is set to new defaults when enabled
- Attachment Hand Menu is incorrectly rotated

### Known issues
- Repeatedly opening scenes can cause memory use increase
- The OpenXR Leap Provider does not currently support the `Confidence` hand property (and will return fixed values)
- After using Ultraleap OpenXR in Unity Editor, the tracking mode of device 0 will be set to HMD until the Unity Editor session ends. This can stop the testing of non-XR scenes until the Unity Editor is re-opened

## [6.12.1] - 28/09/23

### Tracking Client versions
- Windows 	v5.16.0
- MacOS 	v5.16.0
- Android 	v5.16.0

### Fixed
- (leapXRServiceProvider) Hands offset incorrectly on Windows when using Leap 2

### Known issues 
- Use of the LeapCSharp Config class is unavailable with v5.X tracking service
- Repeatedly opening scenes can cause memory use increase
- Currently the Ultraleap Hand Tracking feature for OpenXR requires the New and Legacy input systems to be enabled, to simultaneously use OpenXR and the Ultraleap Unity Plugin's features.
- The OpenXR Leap Provider does not currently support the `Confidence` hand property (and will return fixed values)
- After using Ultraleap OpenXR in Unity Editor, the tracking mode of device 0 will be set to HMD until the Unity Editor session ends. This can stop the testing of non-XR scenes until the Unity Editor is re-opened
- Running both the Ultraleap XRHands Subsystem and another XRHands Subsystem at the same time causes unstable results. Only enable one at a time.


## [6.12.0] - 12/09/23

### Added
- (MRTK Support) Added an MRTK3 subsystem for using Leap tracking directly (non-OpenXR)

### Changed
- (XRHands) XRHands subsystem will now use existing LeapXRServiceProviders found in the scene before considering generating new ones

### Fixed
- (XRHands) XRHands double-translates tracking data causing XRI InputActions to be wrongly positioned when the XROrigin is moved

### Known issues 
- Use of the LeapCSharp Config class is unavailable with v5.X tracking service
- Repeatedly opening scenes can cause memory use increase
- Currently the Ultraleap Hand Tracking feature for OpenXR requires the New and Legacy input systems to be enabled, to simultaneously use OpenXR and the Ultraleap Unity Plugin's features.
- The OpenXR Leap Provider does not currently support the `Confidence` hand property (and will return fixed values)
- After using Ultraleap OpenXR in Unity Editor, the tracking mode of device 0 will be set to HMD until the Unity Editor session ends. This can stop the testing of non-XR scenes until the Unity Editor is re-opened
- The OpenXR Leap Provider palm can be in unexpected position when using pre-1.4.3 OpenXR Layer. A workaround is to ensure you use 1.4.3 or newer - installed by the 5.12.0 or newer Tracking Service Installer
- Running both the Ultraleap XRHands Subsystem and another XRHands Subsystem at the same time causes unstable results. Only enable one at a time.


## [6.12.0] - 12/09/23

### Added
- (MRTK Support) Added an MRTK3 subsystem for using Leap tracking directly (non-OpenXR)

### Changed
- (XRHands) XRHands subsystem will now use existing LeapXRServiceProviders found in the scene before considering generating new ones

### Fixed
- (XRHands) XRHands double-translates tracking data causing XRI InputActions to be wrongly positioned when the XROrigin is moved

### Known issues 
- Use of the LeapCSharp Config class is unavailable with v5.X tracking service
- Repeatedly opening scenes can cause memory use increase
- Currently the Ultraleap Hand Tracking feature for OpenXR requires the New and Legacy input systems to be enabled, to simultaneously use OpenXR and the Ultraleap Unity Plugin's features.
- The OpenXR Leap Provider does not currently support the `Confidence` hand property (and will return fixed values)
- After using Ultraleap OpenXR in Unity Editor, the tracking mode of device 0 will be set to HMD until the Unity Editor session ends. This can stop the testing of non-XR scenes until the Unity Editor is re-opened
- The OpenXR Leap Provider palm can be in unexpected position when using pre-1.4.3 OpenXR Layer. A workaround is to ensure you use 1.4.3 or newer - installed by the 5.12.0 or newer Tracking Service Installer
- Running both the Ultraleap XRHands Subsystem and another XRHands Subsystem at the same time causes unstable results. Only enable one at a time.


## [6.11.0] - 14/08/23

### Added
- (Physics Hands) Finger displacement values to each finger
- (Physics Hands) Interface based events for easier development
    - Please see the updated scripts in the Physics Hands example scene for more information
- (HandRays) Add methods to invoke handray Frame & State Change in inherited classes
- (LeapXRServiceProvider) Use of device transforms from the service when using Default device offset mode. This does not include tilt/rotation

### Changed
- (Physics Hands) Burst compute is now used to improve certain physics calculation performance
    - In Unity 2020+ this is used for "hand is colliding" functions only
	- In Unity 2022+ all collision functions are handled by Burst
- (Physics Hands) Parameters of the hand (e.g. contact distance) are now controlled at the provider level and have adjusted defaults for better interactions
- (Physics Hands) OnObjectStateChange event has been replaced with SubscribeToStateChanges
    - This is tailored to handle specific Rigidbodies and will only fire when your subscribed Rigidbody is affected
- (Physics Hands) Hand and bone states have been improved and are more consistent with expectations
- (Physics Hands) Updated example scene with new events and better visuals
- (Physics Hands) Updated PhysicsBone IsObjectGrabbable calculations to use the closest point on the bone to the hovered object
- (Physics Hands) Improved Physics Hands grasp helpers to take into account grabs where bones are facing each other
- (Locomotion) Expose teleport anchor list & last teleported anchor
- (Locomotion) Moved Jump Gems further away from the arm, to account for sleeves
- (Locomotion) Added functionality to update the initial position and rotations of the TP anchor after the first Awake

### Fixed
- (Physics Hands) Hand forces are reduced when pushing into objects with fingers
- (Physics Hands) Stopped physics buttons from rotating incorrectly
- (Locomotion) Jump Gems look for audio sources in their children, even if the audio source was set
- (Locomotion) If pinched gem was null, jump gem teleport could still be in a selected state
- (Locomotion) Teleport ray did not change to an invalid colour when no colliders were hit
- (Core) Fixed hands juddering in XR when interpolation is turned off on the LeapXRServiceProvider. Turning off interpolation now turns off head pose interpolation
- (PoseViewer) Pose viewer rotation does not match the targets rotation

### Known issues 
- Use of the LeapCSharp Config class is unavailable with v5.X tracking service
- Repeatedly opening scenes can cause memory use increase
- Currently the Ultraleap Hand Tracking feature for OpenXR requires the New and Legacy input systems to be enabled, to simultaneously use OpenXR and the Ultraleap Unity Plugin's features.
- The OpenXR Leap Provider does not currently support the `Confidence` hand property (and will return fixed values)
- After using Ultraleap OpenXR in Unity Editor, the tracking mode of device 0 will be set to HMD until the Unity Editor session ends. This can stop the testing of non-XR scenes until the Unity Editor is re-opened
- The OpenXR Leap Provider palm can be in unexpected position when using pre-1.4.3 OpenXR Layer. A workaround is to ensure you use 1.4.3 or newer - installed by the 5.12.0 or newer Tracking Service Installer
- Running both the Ultraleap XRHands Subsystem and another XRHands Subsystem at the same time causes unstable results. Only enable one at a time.

## [6.10.0] - 05/07/23

This release was tested against Unity 2021.3 LTS and 2022.3 LTS

### Added
- (XRHands) Direct Leap XRHands Subsystem
- (UltraleapSettings) Ultraleap Settings ScriptableObject to toggle features
- (InputSystem) A XRHands to Leap InputActions and Meta Aim InputActions converter

### Changed
- (Hand Pose) Removed unnecessary Hand Pose scriptable context menu option
- (Multi device aggregation) Added wrist and arm to ConfidenceInterpolation aggregator
- (Preview XRI) Removed XRI content from Preview Package in favour of XRHands+XRI content in Tracking Package. Follow the Ultraleap [XR Docs](https://docs.ultraleap.com/unity-api/The-Examples/XR/index.html) for XRI and XRHands to get started

### Fixed
- (Preview) CPU performance issues on PICO when re-connecting tracking device
- (Locomotion) Jump gems could occasionally break and not show their ray
- VectorHand bone directions and thumb rotations

### Known issues 
- Use of the LeapCSharp Config class is unavailable with v5.X tracking service
- Repeatedly opening scenes can cause memory use increase
- Currently the Ultraleap Hand Tracking feature for OpenXR requires the New and Legacy input systems to be enabled, to simultaneously use OpenXR and the Ultraleap Unity Plugin's features.
- The OpenXR Leap Provider does not currently support the `Confidence` hand property (and will return fixed values)
- If using OpenXR when using Unity 2020 and Ultraleap Tracking Plugin via .unitypackage, an error will appear on import relating to HandTrackingFeature. This has been fixed by Unity on Unity 2021
	- A workaround is to right click on \ThirdParty\Ultraleap\Tracking\OpenXR\Runtime\Scripts\HandTrackingFeature.cs and select 'Reimport'
- After using Ultraleap OpenXR in Unity Editor, the tracking mode of device 0 will be set to HMD until the Unity Editor session ends. This can stop the testing of non-XR scenes until the Unity Editor is re-opened
- The OpenXR Leap Provider palm can be in unexpected position when using pre-1.4.3 OpenXR Layer. A workaround is to ensure you use 1.4.3 or newer - installed by the 5.12.0 or newer Tracking Service Installer
- Running both the Ultraleap XRHands Subsystem and another XRHands Subsystem at the same time causes unstable results. Only enable one at a time.

## [6.9.0] - 08/06/23

### Added
- (Physics Hands) In-editor readme for example scene
- (Attachment Hands) Predicted pinch position
- (LeapServiceProvider) Ability to change the number of Service connection attempts and interval


### Changed
- (HandUtils) Only cache static Provider and CameraRig references when they are requested
- (HandUtils) Mark Provider-dependant methods as obsolete and point to suitable replacements
- (UIInput) Cursors are disabled by default and enabled when required
- (LeapXRServiceProvider) When using Default offset, updated values will be used automatically
- (Utils) All references to Utils in the Plugin specify Leap.Unity.Utils to avoid clashes with other Utils classes

### Fixed
- (OpenXRProvider) Hand `Rotation`, `Direction`, `PalmPosition`, `PalmNormal` and `StabilisedPalmPosition` do not match LeapC when using OpenXR layer 1.4.4
- (OpenXRProvider) Elbow length incorrectly calculated.
- (OpenXRProvider) Finger `Direction` is incorrectly set to the tip bone direction rather than the intermediate
- (OpenXRProvider) Hand `GrabStrength` is computed before all required information is available
- (UIInput) When hand lost or leaves canvas near hovered button, button stays hovered


### Known issues 
- Use of the LeapCSharp Config class is unavailable with v5.X tracking service
- Repeatedly opening scenes can cause memory use increase
- Currently the Ultraleap Hand Tracking feature for OpenXR requires the New and Legacy input systems to be enabled, to simultaneously use OpenXR and the Ultraleap Unity Plugin's features.
- The OpenXR Leap Provider does not currently support the `Confidence` hand property (and will return fixed values)
- If using OpenXR when using Unity 2020 and Ultraleap Tracking Plugin via .unitypackage, an error will appear on import relating to HandTrackingFeature. This has been fixed by Unity on Unity 2021
	- A workaround is to right click on \ThirdParty\Ultraleap\Tracking\OpenXR\Runtime\Scripts\HandTrackingFeature.cs and select 'Reimport'
- After using Ultraleap OpenXR in Unity Editor, the tracking mode of device 0 will be set to HMD until the Unity Editor session ends. This can stop the testing of non-XR scenes until the Unity Editor is re-opened
- The OpenXR Leap Provider palm can be in unexpected position when using pre-1.4.3 OpenXR Layer. A workaround is to ensure you use 1.4.3 or newer - installed by the 5.12.0 or newer Tracking Service Installer

## [6.8.1] - 19/05/23

### Added
- Runtime unit tests for pose detection

### Changed
- Changed OpenXRLeapProvider to calculate a LeapC-style palm width and pinch strength

### Fixed
- Preview package version dependency mismatch for XRI when using InputSystem 1.4.4
- (Passthrough) change the handling of the distortion matrix for vertical alignment across device types

### Known issues 
- Scenes containing the infrared viewer render incorrectly on Android build targets and in scriptable render pipelines such as URP and HDRP. 
- Use of the LeapCSharp Config class is unavailable with v5.X tracking service
- Repeatedly opening scenes can cause memory use increase
- Currently the Ultraleap Hand Tracking feature for OpenXR requires the New and Legacy input systems to be enabled, to simultaneously use OpenXR and the Ultraleap Unity Plugin's features.
- The OpenXR Leap Provider does not currently support the `Confidence` hand property (and will return fixed values)
- If using OpenXR when using Unity 2020 and Ultraleap Tracking Plugin via .unitypackage, an error will appear on import relating to HandTrackingFeature. This has been fixed by Unity on Unity 2021
	- A workaround is to right click on \ThirdParty\Ultraleap\Tracking\OpenXR\Runtime\Scripts\HandTrackingFeature.cs and select 'Reimport'
- DrawMeshInstanced error log on certain Unity versions when using Capsule Hands. [Unity Issue Tracker](https://issuetracker.unity3d.com/issues/drawmeshinstanced-does-not-support-dot-dot-dot-error-in-the-console-pops-up-when-the-shader-does-support-instanced-rendering)
- After using Ultraleap OpenXR in Unity Editor, the tracking mode of device 0 will be set to HMD until the Unity Editor session ends. This can stop the testing of non-XR scenes until the Unity Editor is re-opened

## [6.8.0] - 12/05/23

### Added
- (Anchorable Behaviour) Code to automatically create a basic curve for attraction reach distance on new instance of the script
- (Interaction Engine) New options to the create menu under "Interaction", can now create:
	- Interaction Cube, 3D Button, 3D UI panel, Anchor, Anchorable Object, Attachment Hand Menu, Interaction Manager
- (Physics Hands) Added function to check if a grasped object has been pinched
- Presets to Capsule Hands to be able to change the way they look easily
- Options for Capsule Hands to change disable particular joints
- Option to show Upper Arm for Capsule Hands. (Works best in XR)
- (Preview) XRI implementation now supports more Input Actions similar to that of OpenXRs Interaction Profiles

### Changed
- (Anchorable Behaviour) Ability to change the speed at which an object is attracted to the hand
- (Physics Hands) Significantly improved palm latency
- (Physics Hands) Reduced overall forces of hands and fingers
- (Physics Hands) Improved object weight movement (less wobbly, overall faster and more predictable)
- (Physics Hands) Improved contact information of helpers
- (Physics Hands) Removed grasp distance
- (Physics Hands) Removed "strength" from the provider and replaced with per hand velocity limits
- (Physics Hands) Grasp helpers now modify object mass on grasp and restore it on release
- (Hand Rays) TransformWristShoulderRay interpolates direction, rather than aim position
- LeapXRServiceProvider Default offset mode now uses known device transforms or falls back to a default value
- (Interaction) Grab ball now has an option to Continuously restrict the grab balls distance from the player. This allows grab balls to follow the player
- Leap provider can now be set manually in leap provider manager
- Removed Infrared Viewer example scene and prefab

### Fixed
- (Physics Hands) Fixed joints exploding when teleporting the hand for one frame
- (Physics Hands) Fixed wrist position becoming misaligned over time

### Known issues 
- Scenes containing the infrared viewer render incorrectly on Android build targets and in scriptable render pipelines such as URP and HDRP. 
- Use of the LeapCSharp Config class is unavailable with v5.X tracking service
- Repeatedly opening scenes can cause memory use increase
- Currently the Ultraleap Hand Tracking feature for OpenXR requires the New and Legacy input systems to be enabled, to simultaneously use OpenXR and the Ultraleap Unity Plugin's features.
- The OpenXR Leap Provider does not currently support the `Confidence` hand property (and will return fixed values)
- If using OpenXR when using Unity 2020 and Ultraleap Tracking Plugin via .unitypackage, an error will appear on import relating to HandTrackingFeature. This has been fixed by Unity on Unity 2021
	- A workaround is to right click on \ThirdParty\Ultraleap\Tracking\OpenXR\Runtime\Scripts\HandTrackingFeature.cs and select 'Reimport'
- DrawMeshInstanced error log on certain Unity versions when using Capsule Hands. [Unity Issue Tracker](https://issuetracker.unity3d.com/issues/drawmeshinstanced-does-not-support-dot-dot-dot-error-in-the-console-pops-up-when-the-shader-does-support-instanced-rendering)
- After using Ultraleap OpenXR in Unity Editor, the tracking mode of device 0 will be set to HMD until the Unity Editor session ends. This can stop the testing of non-XR scenes until the Unity Editor is re-opened

## [6.7.0] - 3/4/23

### Added
- Brand new pose detection feature!
	- Pose Detector (Invokes events when the chosen pose is made)
	- Pose Detection Library (A library of pre-defined poses)
		- Thumbs up, OK, Point, Open palm, Fist, Horns
	- Pose Recorder (To record poses of your own)
	- New Example Scenes
		- Pose Recorder (A scene set up for you to record poses of your own)
		- Pose Detection (A barebones scene to show how you can use the detector in your own scenes)
		- Pose Showcase (To view and try out our new library of poses)
			- Thumbs up, Thumbs down, OK, Point, Open palm up, Open palm down, Fist, Horns
- (Physics Hands) Warnings for gravity and timestep settings
- (Physics Hands) Exposed interaction mask
- (Physics Hands) Dynamically adjusting fingers when grabbing objects
- (Physics Hands) Distance calculations and values for each bone
- Per finger pinch distances in HandUtils
- Ability for AnchorableBehaviours to attach on demand
- Added advanced option to LeapXRServiceProvider to avoid adding TrackedPoseDrivers to MainCamera
- Ability to clear all attachments on AttachmentHands component
- (UIInput) Added an option to hide the pointer cursor when hands are interacting with UI elements in direct/tactile mode

### Changed
- (Physics Hands) Reduced hand to object collision radius when throwing and testing overlaps
- (Physics Hands) Thumb joints reverted to revolute for non-0th thumb joints
- (Physics Hands) Default physics hands solver iterations & presets
- (Physics Hands) Heuristic calculations moved to WaitForFixedUpdate
- (Physics Hands) Non-0th joints are now only revolute once again
- Reordered example assets to make it easier to traverse


### Fixed
- (Physics Hands) Bone directions when converting back to Leap Hands
- (Physics Hands) Incorrect setup and positioning of physics buttons
- 0th thumb bone rotation values when using OpenXR
- "Pullcord" jitters when being interacted with
- Hand Binder finger tip scale disproportionately when LeapProvider Transform is scaled
- Creating objects from GameObject/Ultraleap/ menu makes more than just prefabs - Ghost Hands (with Arms)
- (UIInput) several interaction events not firing when both direct and indirect interaction is enabled 
- Incorrect warning of duplicate InteractionHands in InteractionManager

### Known issues 
- Scenes containing the infrared viewer render incorrectly on Android build targets and in scriptable render pipelines such as URP and HDRP. 
- Use of the LeapCSharp Config class is unavailable with v5.X tracking service
- Repeatedly opening scenes can cause memory use increase
- Currently the Ultraleap Hand Tracking feature for OpenXR requires the New and Legacy input systems to be enabled, to simultaneously use OpenXR and the Ultraleap Unity Plugin's features.
- The OpenXR Leap Provider does not currently support the `Confidence` hand property (and will return fixed values)
- If using OpenXR when using Unity 2020 and Ultraleap Tracking Plugin via .unitypackage, an error will appear on import relating to HandTrackingFeature. This has been fixed by Unity on Unity 2021
	- A workaround is to right click on \ThirdParty\Ultraleap\Tracking\OpenXR\Runtime\Scripts\HandTrackingFeature.cs and select 'Reimport'
- DrawMeshInstanced error log on certain Unity versions when using Capsule Hands. [Unity Issue Tracker](https://issuetracker.unity3d.com/issues/drawmeshinstanced-does-not-support-dot-dot-dot-error-in-the-console-pops-up-when-the-shader-does-support-instanced-rendering)
- After using Ultraleap OpenXR in Unity Editor, the tracking mode of device 0 will be set to HMD until the Unity Editor session ends. This can stop the testing of non-XR scenes until the Unity Editor is re-opened

## [6.6.0] - 17/02/23

### Added
- Interaction Grab Ball for 3D UI

### Changed
- Capsule Hands pinky metacarpal position to better represent the actual joint position
- Reduced performance overhead of OpenXRLeapProvider
- Reduced performance overhead when accessing Hands via Hands.Right, Hands.Left and Hands.GetHand
- Reduced performance overhead when transforming Leap.Frames, Leap.Hands and Leap.Bones using Basis
- Reduced performance overhead when accessing scale in Capsule Hands
- Reduced performance overhead when using Preview UI Input Cursor

### Fixed
- Preview package example scene referencing a prefab in the non-preview package

### Known issues 
- Scenes containing the infrared viewer render incorrectly on Android build targets and in scriptable render pipelines such as URP and HDRP. 
- Use of the LeapCSharp Config class is unavailable with v5.X tracking service
- Repeatedly opening scenes can cause memory use increase
- Currently the Ultraleap Hand Tracking feature for OpenXR requires the New and Legacy input systems to be enabled, to simultaneously use OpenXR and the Ultraleap Unity Plugin's features.
- The OpenXR Leap Provider does not currently support the `Confidence` hand property (and will return fixed values)
- If using OpenXR when using Unity 2020 and Ultraleap Tracking Plugin via .unitypackage, an error will appear on import relating to HandTrackingFeature. This has been fixed by Unity on Unity 2021
	- A workaround is to right click on \ThirdParty\Ultraleap\Tracking\OpenXR\Runtime\Scripts\HandTrackingFeature.cs and select 'Reimport'
- DrawMeshInstanced error log on certain Unity versions when using Capsule Hands. [Unity Issue Tracker](https://issuetracker.unity3d.com/issues/drawmeshinstanced-does-not-support-dot-dot-dot-error-in-the-console-pops-up-when-the-shader-does-support-instanced-rendering)
- After using Ultraleap OpenXR in Unity Editor, the tracking mode of device 0 will be set to HMD until the Unity Editor session ends. This can stop the testing of non-XR scenes until the Unity Editor is re-opened

## [6.5.0] - 26/01/23

### Added
- Public toggle for interpolation on LeapServiceProviders
- Hands prefabs added to GameObject/Ultraleap/Hands create menu
- Action-based XRI implementation with Example scene in Preview package
- Added const S_TO_US as replacement for incorrectly named S_TO_NS in LeapServiceProvider
- Check box in Hand Binder under fine tuning options to enable or disable moving the elbow based on forearm scale

### Changed
- "Hands.Provider" static function now searches for subjectively the best LeapProvider available in the scene. Will use PostProcessProvider first rather than LeapServiceProvider
- Removed the OVRProvider from the Preview Package. This is now achievable via the OpenXR Provider in the main Tracking Package

### Fixed
- Offset between skeleton hand wrist and forearm in sample scenes
- OpenXRLeapProvider CheckOpenXRAvailable has a nullref when XRGeneralSettings activeloader is not set up
- XrLeapProviderManager initialising when there is no active XR Loader - [Github Issue 1360](https://github.com/ultraleap/UnityPlugin/issues/1360)
- OnAnchorDisabled not being called when an Anchor gameobject is disabled
- Documentation for Finger.Direction says it is tip direction but should say intermediate direction
- OpenXR thumb joint rotation offsets do not align with LeapC expectations

### Known issues 
- Scenes containing the infrared viewer render incorrectly on Android build targets and in scriptable render pipelines such as URP and HDRP. 
- Use of the LeapCSharp Config class is unavailable with v5.X tracking service
- Repeatedly opening scenes can cause memory use increase
- Currently the Ultraleap Hand Tracking feature for OpenXR requires the New and Legacy input systems to be enabled, to simultaneously use OpenXR and the Ultraleap Unity Plugin's features.
- The OpenXR Leap Provider does not currently support the `Confidence` hand property (and will return fixed values)
- If using OpenXR when using Unity 2020 and Ultraleap Tracking Plugin via .unitypackage, an error will appear on import relating to HandTrackingFeature. This has been fixed by Unity on Unity 2021
	- A workaround is to right click on \ThirdParty\Ultraleap\Tracking\OpenXR\Runtime\Scripts\HandTrackingFeature.cs and select 'Reimport'
- DrawMeshInstanced error log on certain Unity versions when using Capsule Hands. [Unity Issue Tracker](https://issuetracker.unity3d.com/issues/drawmeshinstanced-does-not-support-dot-dot-dot-error-in-the-console-pops-up-when-the-shader-does-support-instanced-rendering)
- After using Ultraleap OpenXR in Unity Editor, the tracking mode of device 0 will be set to HMD until the Unity Editor session ends. This can stop the testing of non-XR scenes until the Unity Editor is re-opened

## [6.4.0] - 05/01/23

### Added
- Pinch To Paint example scene
- Explanation text to all XR example scenes
- Turntable and Pullcord example scene
- Locomotion teleportation system and example scenes in Preview Package

### Fixed
- Android Manifest auto-population when building for OpenXR always adds permissions
- OpenXR finger lengths wrongly include metacarpal lengths
- On contact start and end being called every 20 frames when only 1 bone is colliding

### Known issues 
- Offset between skeleton hand wrist and forearm in sample scenes
- Outline hands aren't displaying
- Scenes containing the infrared viewer render incorrectly on Android build targets and in scriptable render pipelines such as URP and HDRP. 
- Interactions callback scene allows blocks to be moved without doing a grasp pose. 
- Capsule hands don't have a joint colour in HDRP 
- Use of the LeapCSharp Config class is unavailable with v5.X tracking service
- Repeatedly opening scenes can cause memory use increase
- Currently the Ultraleap Hand Tracking feature for OpenXR requires the New and Legacy input systems to be enabled, to simultaneously use OpenXR and the Ultraleap Unity Plugin's features.
- The OpenXR Leap Provider does not currently support the `Confidence` hand property (and will return fixed values)
- If using OpenXR with OpenXR package imported, when using Unity 2020 and Ultraleap Tracking Plugin via .unitypackage, an error will appear on import relating to HandTrackingFeature. This has been fixed by Unity on Unity 2021
	- A workaround is to right click on \ThirdParty\Ultraleap\Tracking\OpenXR\Runtime\Scripts\HandTrackingFeature.cs and select 'Reimport'
- DrawMeshInstanced error log on certain Unity versions when using Capsule Hands. [Unity Issue Tracker](https://issuetracker.unity3d.com/issues/drawmeshinstanced-does-not-support-dot-dot-dot-error-in-the-console-pops-up-when-the-shader-does-support-instanced-rendering)
- After using Ultraleap OpenXR in Unity Editor, the tracking mode of device 0 will be set to HMD until the Unity Editor session ends. This can stop the testing of non-XR scenes until the Unity Editor is re-opened

## [6.3.0] - 02/12/22

### Added
- Added XRLeapProviderManager script and Prefab which auto-selects a LeapXRServiceProvider or OpenXRLeapProvider depending on the availability of OpenXR
- Added GetChirality extension method to hand which returns the Chirality enum of the hand
- Added ability to change HandBinder scaling speed

### Changed
- Reduced the contact offset for Interaction Hands colliders so contact is closer

### Fixed
- Check for main camera being null in (get) EditTimeFrame in OpenXRLeapProvider
- Detector null reference error when creating a detector at runtime
- InteractionSlider now raises event for value changes when setting values via the Horizontal and Vertical Percent properties
- XRServiceProvider and OpenXRLeapProvider do not scale when the player scales
- `timeVisible` was not populated on the OpenXR Provider for `Finger`s
- Fix issue with generic hand-shader giving compile errors in some circumstances

### Known issues 
- Offset between skeleton hand wrist and forearm in sample scenes
- Outline hands aren't displaying
- Scenes containing the infrared viewer render incorrectly on Android build targets and in scriptable render pipelines such as URP and HDRP. 
- Interactions callback scene allows blocks to be moved without doing a grasp pose. 
- Capsule hands don't have a joint colour in HDRP 
- Use of the LeapCSharp Config class is unavailable with v5.X tracking service
- Repeatedly opening scenes can cause memory use increase
- Currently the Ultraleap Hand Tracking feature for OpenXR requires the New and Legacy input systems to be enabled, to simultaneously use OpenXR and the Ultraleap Unity Plugin's features.
- The OpenXR Leap Provider does not currently support the `Confidence` hand property (and will return fixed values)
- If using OpenXR with OpenXR package imported, when using Unity 2020 and Ultraleap Tracking Plugin via .unitypackage, an error will appear on import relating to HandTrackingFeature. This has been fixed by Unity on Unity 2021
	- A workaround is to right click on \ThirdParty\Ultraleap\Tracking\OpenXR\Runtime\Scripts\HandTrackingFeature.cs and select 'Reimport'
- DrawMeshInstanced error log on certain Unity versions when using Capsule Hands. [Unity Issue Tracker](https://issuetracker.unity3d.com/issues/drawmeshinstanced-does-not-support-dot-dot-dot-error-in-the-console-pops-up-when-the-shader-does-support-instanced-rendering)
- After using Ultraleap OpenXR in Unity Editor, the tracking mode of device 0 will be set to HMD until the Unity Editor session ends. This can stop the testing of non-XR scenes until the Unity Editor is re-opened

## [6.2.1] - 07/10/2022

### Fixed
- Fixed `DeviceID`, `Timestamp` and `CurrentFramesPerSecond` for `Frames` from the OpenXR Provider

### Known issues 
- Offset between skeleton hand wrist and forearm in sample scenes
- Outline hands aren't displaying
- Scenes containing the infrared viewer render incorrectly on Android build targets and in scriptable render pipelines such as URP and HDRP. 
- Interactions callback scene allows blocks to be moved without doing a grasp pose. 
- Capsule hands don't have a joint colour in HDRP 
- Use of the LeapCSharp Config class is unavailable with v5.X tracking service
- Repeatedly opening scenes can cause memory use increase
- Currently the Ultraleap Hand Tracking feature for OpenXR requires the New and Legacy input systems to be enabled, to simultaneously use OpenXR and the Ultraleap Unity Plugin's features.
- The OpenXR Leap Provider does not currently support the `Confidence` hand property (and will return fixed values)
- If using OpenXR with OpenXR package imported, when using Unity 2020 and Ultraleap Tracking Plugin via .unitypackage, an error will appear on import relating to HandTrackingFeature. This has been fixed by Unity on Unity 2021
	- A workaround is to right click on \ThirdParty\Ultraleap\Tracking\OpenXR\Runtime\Scripts\HandTrackingFeature.cs and select 'Reimport'

## [6.2.0] - 23/09/2022

### Added
- Getting Started example scene
- Mesh Hands example scenes for XR

### Changed
- Reorganised example scenes for more clarity
- Removed HDRP hands example scenes

### Fixed
- Fixed compile error with GenericHandShader's use of TRANSFER_SHADOW

### Known issues 
- Offset between skeleton hand wrist and forearm in sample scenes
- Outline hands aren't displaying
- Scenes containing the infrared viewer render incorrectly on Android build targets and in scriptable render pipelines such as URP and HDRP. 
- Interactions callback scene allows blocks to be moved without doing a grasp pose. 
- Capsule hands don't have a joint colour in HDRP 
- Use of the LeapCSharp Config class is unavailable with v5.X tracking service
- Repeatedly opening scenes can cause memory use increase
- Currently the Ultraleap Hand Tracking feature for OpenXR requires the New and Legacy input systems to be enabled, to simultaneously use OpenXR and the Ultraleap Unity Plugin's features.
- The OpenXR Leap Provider does not currently support the `Confidence` hand property (and will return fixed values)
- If using OpenXR with OpenXR package imported, when using Unity 2020 and Ultraleap Tracking Plugin via .unitypackage, an error will appear on import relating to HandTrackingFeature. This has been fixed by Unity on Unity 2021
	- A workarond is to right click on \ThirdParty\Ultraleap\Tracking\OpenXR\Runtime\Scripts\HandTrackingFeature.cs and select 'Reimport'

## [6.1.0] - 09/09/2022

### Added
- Device-Specific RectilinearToPixelEx method
- OpenXR into a conditionally included asmdef taht automatically removes OpenXR Package if necessary

### Fixed
- Tracking Binding is lost when reloading scenes on Android
- AttachmentHands can get in a popup loop when resetting the component
- RectilinearToPixel returns NaN

### Known issues 
- Scenes containing the infrared viewer render incorrectly on Android build targets and in scriptable render pipelines such as URP and HDRP. 
- Interactions callback scene allows blocks to be moved without doing a grasp pose. 
- Capsule hands don't have a joint colour in HDRP 
- Use of the LeapCSharp Config class is unavailable with v5.X tracking service
- Repeatedly opening scenes can cause memory use increase
- Currently the Ultraleap Hand Tracking feature for OpenXR requires the New and Legacy input systems to be enabled, to simultaneously use OpenXR and the Ultraleap Unity Plugin's features.
- The OpenXR Leap Provider does not currently support the `Confidence` hand property (and will return fixed values)
- If using OpenXR with OpenXR package imported, when using Unity 2020 and Ultraleap Tracking Plugin via .unitypackage, an error will appear on import relating to HandTrackingFeature. This has been fixed by Unity on Unity 2021
	- A workarond is to right click on \ThirdParty\Ultraleap\Tracking\OpenXR\Runtime\Scripts\HandTrackingFeature.cs and select 'Reimport'

## [6.0.0] - 17/08/2022

### Added
- Added a low poly hand model with an arm
- Added create menu options for LeapServiceProviders via GameObject/Ultrealeap/Service Provider (X)
- Added TrackedPoseDriver to all XR example scenes
- Added ability to create LeapServiceProviders from the GameObject/Create menu in editor
- Added Hand Rays to Preview package

### Changed
- Cleaned up the image retriever and LeapServiceProvider Execution order, reducing unnecessary service and log messages
- ImageRetriever prefab and LeapEyeDislocator.cs (formerly used for passthrough) removed and replaced by 'VR Infrared Camera' prefab in the Tracking Examples package
- Example scenes URL
- Hand rigs bones have their  'L and R' prefixes removed
- Removed Hotkey functionality
- Removed use of obsolete methods
- Removed obsolete methods
- Removed pre-2020LTS specific support
- Removed use of SVR
- Changed use of Vector and LeapQuaternion in favour of Vector3 and Quaternion
- Removed Legacy XR support
- Removed MainCaneraProvider in favour of Camera.Main
- All units to be in M rather than MM when getting hand data

### Fixed

- HandBinder scales hands in edit mode when there is no LeapServiceProvider in the scene
- Leap.Controller.InternalFrameReady, LeapInternalFrame is never dispatched
- HandUI example scene panel exists after hand lost
- ChangeTrackingMode and GetTrackingMode on LeapServiceProvider fail when in disabled multi-device mode
- FOV Gizmos are not visible when opening an example scene containing a Service Provider in multiDeviceMode = disabled.
- FOV Gizmos are not visible when changing render pipelines
- AttachmentHands untick bone in inspector UI causes looping error when deleting gameobject in edit mode
- SpatialTracking dependency errors

### Known issues 
- Scenes containing the infrared viewer render incorrectly on Android build targets and in scriptable render pipelines such as URP and HDRP. 
- Interactions callback scene allows blocks to be moved without doing a grasp pose. 
- Capsule hands don't have a joint colour in HDRP 
- Use of the LeapCSharp Config class is unavailable with v5.X tracking service
- Repeatedly opening scenes can cause memory use increase

## [5.13.1] - 26/08/2022

### Announcements

In line with Unity's end of support of Unity 2019 LTS, we will no longer be actively supporting Unity 2019.

We will also be deprecating some functionality and moving core utilities into a separate package.

If you are using classes and methods that are marked as obsolete and will be moved to the new legacy package without a replacement, you may wish to use "#pragma warning disable 0618" at the start and "#pragma warning restore 0618" at the end of your method that makes use of it to suppress the warnings.

If you have any concerns about this, please contact us on [Github Discussions](https://github.com/ultraleap/UnityPlugin/discussions)

### Fixed
- Tracking Binding is lost when reloading scenes on Android

### Known issues 
- Scenes containing the infrared viewer render incorrectly on Android build targets and in scriptable render pipelines such as URP and HDRP. 
- Demo scenes do not start at the correct height for a seated user. The XR Plugin Management System adjusts the camera height. This means the user has to adjust components in the scene to the correct height - e.g. camera height. Currently our position is to support the legacy XR system height settings. 
- Hands in Desktop scenes can appear far away from the camera 
- Interactions callback scene allows blocks to be moved without doing a grasp pose. 
- Capsule hands don't have a joint colour in HDRP 
- Hand UI can become detached from hand when hand is removed from view
- Multi-device (desktop) Scene camera position can become offset
- FOV visualization does not display after changing render pipelines

## [5.13.0] - 21/07/2022

### Announcements

In line with Unity's end of support of Unity 2019 LTS, we will no longer be actively supporting Unity 2019.

We will also be deprecating some functionality and moving core utilities into a separate package.

If you are using classes and methods that are marked as obsolete and will be moved to the new legacy package without a replacement, you may wish to use "#pragma warning disable 0618" at the start and "#pragma warning restore 0618" at the end of your method that makes use of it to suppress the warnings.

If you have any concerns about this, please contact us on [Github Discussions](https://github.com/ultraleap/UnityPlugin/discussions)

### Added
- Added HandModelManager to the Hands Module - an easy way to enable/disable hand models
- Added option to freeze hand state on HandEnableDisable

### Changed
- Changed Rigged Hand Example scenes to use HandModelManager

### Fixed
- Inertia Hands are very jittery and `hand.TimeVisible` is not accurate
- Compile errors in the Infrared Viewer example scene when using Single Pass Stereo rendering mode

### Known issues 
- Scenes containing the infrared viewer render incorrectly on Android build targets and in scriptable render pipelines such as URP and HDRP. 
- Demo scenes do not start at the correct height for a seated user. The XR Plugin Management System adjusts the camera height. This means the user has to adjust components in the scene to the correct height - e.g. camera height. Currently our position is to support the legacy XR system height settings. 
- Hands in Desktop scenes can appear far away from the camera 
- Interactions callback scene allows blocks to be moved without doing a grasp pose. 
- Capsule hands don't have a joint colour in HDRP 
- Hand UI can become detached from hand when hand is removed from view
- Multi-device (desktop) Scene camera position can become offset
- FOV visualization does not display after changing render pipelines
- Use of the LeapCSharp Config class is unavailable with v5.X tracking service

## [5.12.1] - 06/07/2022

### Announcements

In line with Unity's end of support of Unity 2019 LTS, we will no longer be actively supporting Unity 2019.

We will also be deprecating some functionality and moving core utilities into a separate package.

If you are using classes and methods that are marked as obsolete and will be moved to the new legacy package without a replacement, you may wish to use "#pragma warning disable 0618" at the start and "#pragma warning restore 0618" at the end of your method that makes use of it to suppress the warnings.

If you have any concerns about this, please contact us on [Github Discussions](https://github.com/ultraleap/UnityPlugin/discussions)

This release is a hotfix for the 5.12.0 release. It fixes the XRI package dependency issue which affects the tracking preview package,
 
### Fixed 
- XRI package dependency is resolved when using the Tracking Preview package.

### Known issues 
- Scenes containing the infrared viewer render incorrectly on systems using single pass stereo with the XR plugin system - e.g. Windows Mixed Reality headsets. SteamVR headsets may also default to single pass stereo, showing the same issue. However in this case, the OpenVR settings can be changed to multipass which resolves the problem. 
- Scenes containing the infrared viewer render incorrectly on Android build targets and in scriptable render pipelines such as URP and HDRP. 
- Demo scenes do not start at the correct height for a seated user. The XR Plugin Management System adjusts the camera height. This means the user has to adjust components in the scene to the correct height - e.g. camera height. Currently our position is to support the legacy XR system height settings. 
- Hands in Desktop scenes can appear far away from the camera 
- Interactions callback scene allows blocks to be moved without doing a grasp pose. 
- Capsule hands don't have a joint colour in HDRP 
- Hand UI can become detached from hand when hand is removed from view
- Multi-device (desktop) Scene camera position can become offset
- FOV visualization does not display after changing render pipelines


## [5.12.0] - 04/07/2022

### Announcements

In line with Unity's end of support of Unity 2019 LTS, we will no longer be actively supporting Unity 2019.

We will also be deprecating some functionality and moving core utilities into a separate package.

If you are using classes and methods that are marked as obsolete and will be moved to the new legacy package without a replacement, you may wish to use "#pragma warning disable 0618" at the start and "#pragma warning restore 0618" at the end of your method that makes use of it to suppress the warnings.

If you have any concerns about this, please contact us on [Github Discussions](https://github.com/ultraleap/UnityPlugin/discussions)
 
### Changed
- Various classes and methods have been marked as obsolete in preparation for a major version change in the near future
 
### Fixed 
- VertexOffsetShader displays errors in Unity 2021 due to invalid path
- ThreadAbortException in editor when connecting, most commonly found when using milti-device

### Known issues 
- Scenes containing the infrared viewer render incorrectly on systems using single pass stereo with the XR plugin system - e.g. Windows Mixed Reality headsets. SteamVR headsets may also default to single pass stereo, showing the same issue. However in this case, the OpenVR settings can be changed to multipass which resolves the problem. 
- Scenes containing the infrared viewer render incorrectly on Android build targets and in scriptable render pipelines such as URP and HDRP. 
- Demo scenes do not start at the correct height for a seated user. The XR Plugin Management System adjusts the camera height. This means the user has to adjust components in the scene to the correct height - e.g. camera height. Currently our position is to support the legacy XR system height settings. 
- Possible hand offset issues on XR2 headsets using SVR plugin 
- Hands in Desktop scenes can appear far away from the camera 
- Interactions callback scene allows blocks to be moved without doing a grasp pose. 
- Capsule hands don't have a joint colour in HDRP 
- Hand UI can become detached from hand when hand is removed from view
- Multi-device (desktop) Scene camera position can become offset
- FOV visualization does not display after changing render pipelines


## [5.11.0] - 23/06/2022

### Announcements

In line with Unity's end of support of Unity 2019 LTS, we will no longer be actively supporting Unity 2019.

We will also be deprecating some functionality and moving core utilities into a separate package.

If you have any concerns about this, please contact us on [Github Discussions](https://github.com/ultraleap/UnityPlugin/discussions)
 
### Added 
- Added a hand enable and disable script to the GenericHand_Arm prefab

### Changed
- Changed scale calculations on the Auto-Scale function of the Handbinder, to make it more consistent across different tracking models and more stable when using a hand without metacarpal bones. The scales of all hand prefabs have been slightly changed because of that.
- Disable FOV visualization gizmos by default
- Update minimum Unity version to 2020.3 for UPM packages
 
### Fixed 
- Turning on and off multiple image retrievers referencing the same service provider or the same device results in a very low framerate
- When having two image retrievers that both reference the same device and turning one of them off, then the other one shows a grey image
- Initialising contact for an interaction hand while the hand is not tracked does not work and doesn't attempt again once the hand is tracked
- Attachment Hands Example scene has errors when using a project with InputSystem

### Known issues 
- Scenes containing the infrared viewer render incorrectly on systems using single pass stereo with the XR plugin system - e.g. Windows Mixed Reality headsets. SteamVR headsets may also default to single pass stereo, showing the same issue. However in this case, the OpenVR settings can be changed to multipass which resolves the problem. 
- Scenes containing the infrared viewer render incorrectly on Android build targets and in scriptable render pipelines such as URP and HDRP. 
- Demo scenes do not start at the correct height for a seated user. The XR Plugin Management System adjusts the camera height. This means the user has to adjust components in the scene to the correct height - e.g. camera height. Currently our position is to support the legacy XR system height settings. 
- Possible hand offset issues on XR2 headsets using SVR plugin 
- Hands in Desktop scenes can appear far away from the camera 
- Interactions callback scene allows blocks to be moved without doing a grasp pose. 
- Capsule hands don't have a joint colour in HDRP 
- Hand UI can become detached from hand when hand is removed from view


## [5.10.0] - 10/06/2022

### Announcements

In line with Unity's end of support of Unity 2019 LTS, we will no longer be actively supporting Unity 2019.

We will also be deprecating some functionality and moving core utilities into a separate package.

If you have any concerns about this, please contact us on [Github Discussions](https://github.com/ultraleap/UnityPlugin/discussions)
 
### Added 

- Inform user with a popup error dialog when trying to build for Android without ARM64 set as the only target architecture. User can choose to continue the build if this is intended.

### Changed

- The leapProvider on a handModelBase (eg Capsule Hand) cannot be changed anymore at runtime in the inspector
 
### Fixed 

- Tracking Examples Capsule Hands (VR - Infrared Viewer) scene: hands are aligned with passthrough hands
- After removing XR Service Providers from Transforms, the transform is uneditable

### Known issues 
- Scenes containing the infrared viewer render incorrectly on systems using single pass stereo with the XR plugin system - e.g. Windows Mixed Reality headsets. SteamVR headsets may also default to single pass stereo, showing the same issue. However in this case, the OpenVR settings can be changed to multipass which resolves the problem. 
- Scenes containing the infrared viewer render incorrectly on Android build targets and in scriptable render pipelines such as URP and HDRP. 
- Demo scenes do not start at the correct height for a seated user. The XR Plugin Management System adjusts the camera height. This means the user has to adjust components in the scene to the correct height - e.g. camera height. Currently our position is to support the legacy XR system height settings. 
- Possible hand offset issues on XR2 headsets using SVR plugin 
- Hands in Desktop scenes can appear far away from the camera 
- Interactions callback scene allows blocks to be moved without doing a grasp pose. 
- Capsule hands don't have a joint colour in HDRP 
- Hand UI can become detached from hand when hand is removed from view


## [5.9.0] - 27/05/2022

### Announcements

In line with Unity's end of support of Unity 2019 LTS, we will no longer be actively supporting Unity 2019 following this release.

We will also start deprecating some functionality and moving core utilities into a separate package.

If you have any concerns about this, please contact us on [Github Discussions](https://github.com/ultraleap/UnityPlugin/discussions)
 
### Added 

- Add option to prevent initializing tracking mode for XR service provider 
- Added an option in LeapImageRetriever to hide Rigel device's debug information 
- Enable the use of multiple image retrievers in a scene that correspond to different devices 
- Better visualization for a tracking device’s position and rotation and its FOV as gizmos 

 
### Fixed 

- Automatic Volume visualization does not work in multi device mode 
- Switching between HMD and Screentop using ChangeTrackingMode() briefly switches to Desktop 
- when rendering a passthrough image with OpenGL, the hand visualization is flipped in the undistorted view 
- Changing tracking mode on the same frame as enabling a service provider has no effect 
- Capsule Hands "Cylinder Radius" only updates after hitting play 
- LeapEyeDislocator updates distortion values whenever a new device is plugged in, even if that device is not used for retrieving an image 

### Known issues 
- Scenes containing the infrared viewer render incorrectly on systems using single pass stereo with the XR plugin system - e.g. Windows Mixed Reality headsets. SteamVR headsets may also default to single pass stereo, showing the same issue. However in this case, the OpenVR settings can be changed to multipass which resolves the problem. 
- Scenes containing the infrared viewer render incorrectly on Android build targets and in scriptable render pipelines such as URP and HDRP. 
- Demo scenes do not start at the correct height for a seated user. The XR Plugin Management System adjusts the camera height. This means the user has to adjust components in the scene to the correct height - e.g. camera height. Currently our position is to support the legacy XR system height settings. 
- Possible hand offset issues on XR2 headsets using SVR plugin 
- Hands in Desktop scenes can appear far away from the camera 
- Interactions callback scene allows blocks to be moved without doing a grasp pose. 
- Capsule hands don't have a joint colour in HDRP 
- Hand UI can become detached from hand when hand is removed from view
 

## [5.8.0] - 28/04/2022

### Added
- A Leap Provider can now be specified for attachment hands

### Fixed
- SIR170 Tracking Volume Visualisation was not appearing
- The automatic option on Tracking Volume Visualisation was not working for SIR170s or 3Dis in single device usage
- Unit tests break downstream package dependencies [[#1182]](https://github.com/ultraleap/UnityPlugin/issues/1182)
- reassigned Low Poly Hand material to prefab
- An image from the image Retriever would freeze when switching devices on the relevant Service Provider

### Known issues
- Scenes containing the infrared viewer render incorrectly on systems using single pass stereo with the XR plugin system - e.g. Windows Mixed Reality headsets. SteamVR headsets may also default to single pass stereo, showing the same issue. However in this case, the OpenVR settings can be changed to multipass which resolves the problem.
- Scenes containing the infrared viewer render incorrectly on Android build targets and in scriptable render pipelines such as URP and HDRP.
- Demo scenes do not start at the correct height for a seated user. The XR Plugin Management System adjusts the camera height. This means the user has to adjust components in the scene to the correct height - e.g. camera height. Currently our position is to support the legacy XR system height settings.
- Possible hand offset issues on XR2 headsets using SVR plugin
- Hands in Desktop scenes can appear far away from the camera
- Interactions callback scene allows blocks to be moved without doing a grasp pose.
- Automatic Volume visualization does not work in multi device mode
- Capsule hands don't have a joint colour in HDRP

## [5.7.0] - 19/04/2022

### Added
- Added a new post process provider to distort tracking data to the 3D visuals
- Added the ability to generate a leap hand from a bound hand (Hand Binder) 
- Can now set different tracking optimization modes on tracking devices when running with multiple devices
- method 'GetFingerStrength' in HandUtils, that returns a value indicating how strongly a finger is curled
- Added option to flip image in the passthrough shaders

### Changed
- Policy flags are now handled on a per device basis / contain information about the device they relate to
- ActiveDevice replaced by ActiveDevices. ActiveDevice marked as obsolete
- Legacy SetPolicy/ClearPolicy/IsPolicySet methods on IController marked as obsolete. Use new methods that also take a Device
- In multiple Device Mode = specific, if the specific serial number is null or an empty string, no device is tracking

### Fixed
- Occasional ThreadAbortException on connection polling thread
- Sometimes Frame objects where being constructed without a device ID, even if known
- Multiple device mode remembers device serial numbers after devices are disconnected
- Service provider in multi-device scene does not track using selected device (by serial number) unless it's been selected in the editor
- clear LeapServiceProvider._currentDevice, if the device is unplugged (DeviceLost)

### Known issues
- Scenes containing the infrared viewer render incorrectly on systems using single pass stereo with the XR plugin system - e.g. Windows Mixed Reality headsets. SteamVR headsets may also default to single pass stereo, showing the same issue. However in this case, the OpenVR settings can be changed to multipass which resolves the problem.
- Scenes containing the infrared viewer render incorrectly on Android build targets and in scriptable render pipelines such as URP and HDRP.
- Demo scenes do not start at the correct height for a seated user. The XR Plugin Management System adjusts the camera height. This means the user has to adjust components in the scene to the correct height - e.g. camera height. Currently our position is to support the legacy XR system height settings.
- Possible hand offset issues on XR2 headsets using SVR plugin
- Hands in Desktop scenes can appear far away from the camera
- Interactions callback scene allows blocks to be moved without doing a grasp pose.
- Interactions object scene platform/stage seems to move a lot


## [5.6.0] - 04/04/2022

### Added
- The LeapServiceProvider provides a list of connected devices (LeapServiceProvider.Devices)
- Example scene for multiple devices
- Generic Hand Model that has an Arm and no metacarpal bones (added to example scene 'Rigged Hands (Desktop) (Standard)')
- Accessor for Service version info in the Controller

### Changed
- In 'Multiple Device Mode' = 'Specific', Serial Numbers can be changed at Runtime via the Inspector or via code (new public property LeapServiceProvider.SpecificSerialNumber)
- Exposed SimpleFacingCameraCallbacks.IsFacingCamera in the Interaction Engine
- Allow mesh hands that use the hand binder to be scaled during editor
- Updated the LeapC.dll client to 5.5.0.22-57dcaafe

### Removed

### Fixed
- Lag and stuttering when using multiple devices
- Scene View opens when connecting / disconnecting devices
- Fixed issues with multi-device interpolation failing

### Known issues
- Multiple device mode remembers device serial numbers after devices are disconnected
- Service provider in multi-device scene does not track using selected device (by serial number) unless it's been selected in the editor
- Scenes containing the infrared viewer render incorrectly on systems using single pass stereo with the XR plugin system - e.g. Windows Mixed Reality headsets. SteamVR headsets may also default to single pass stereo, showing the same issue. However in this case, the OpenVR settings can be changed to multipass which resolves the problem.
- Demo scenes do not start at the correct height for a seated user. The XR Plugin Management System adjusts the camera height. This means the user has to adjust components in the scene to the correct height - e.g. camera height. Currently our position is to support the legacy XR system height settings.
- Possible hand offset issues on XR2 headsets using SVR plugin
- Hands in Desktop scenes can appear far away from the camera
- Interactions callback scene allows blocks to be moved without doing a grasp pose.
- Interactions object scene platform/stage seems to move a lot
- Dynamic UI objects throwing backwards most of the time.


## [5.5.0] - 17/03/2022

### Added
- Hand Binder Scale feature, uniformly scale the 3D model model up or down based on the ratio between the leap data and the 3D model. This will require a rebind to calculate the correct scale.
- tracking service version check for multiple device mode. Warning appears if trying to select the 'specific' multi device mode in a service version < 5.3.6

### Changed
- Serial numbers for 'multiple device mode' = 'Specific' can be chosen from a drop down list in the inspector instead of a text field. Using Device indices is no longer supported.

### Removed
- x86 LeapC.dll

### Fixed
- Dynamic UI scene - blocks sometimes did not expand when undocked
-	Capsule hands appear small compared to size of 'IR hands' of user using HDRP / URP and do not line up. Using standard rendering on Unity 2019.4 LTS  hands are usually not visible (but are being tracked). When they appear they do not line up with the hands in the image.
- A check has been added to ensure a subscription to device events won't happen if the leapProvider is null.

### Known issues
-	Scenes containing the infrared viewer render incorrectly on systems using single pass stereo with the XR plugin system - e.g. Windows Mixed Reality headsets. SteamVR headsets may also default to single pass stereo, showing the same issue. However in this case, the OpenVR settings can be changed to multipass which resolves the problem.
-	Demo scenes do not start at the correct height for a seated user. The XR Plugin Management System adjusts the camera height. This means the user has to adjust components in the scene to the correct height - e.g. camera height. Currently our position is to support the legacy XR system height settings.
- Possible hand offset issues on XR2 headsets using SVR plugin
- Hands in Desktop scenes can appear far away from the camera
- Interactions callback scene allows blocks to be moved without doing a grasp pose.
- Interactions object scene platform/stage seems to move a lot
- Dynamic UI objects throwing backwards most of the time.


## [5.4.0] 

### Added
- Basic support for specifying which device a LeapProvider should connect to. Can be specified by device index or serial number. If multiple service providers are in a scene set to use the multiple device mode, they must be set to use the same tracking optimization mode. _(Multiple Device Mode is only supported on the Ultraleap Tracking Service version 5.3.6 and above)_
- Added ability to get / set custom capsule hand colours in code

### Changed
- Updated LeapC.dll client to latest service release. Service supports multiple devices.

### Removed

### Fixed
- Fixed issue with incorrect enum ordering in eLeapEventType (now matches LeapC.h ordering). Inserted eLeapEventType_TrackingMode
- Service Providers not referenced in Hand Post-Process example scene

### Known issues
-	Scenes containing the infrared viewer render incorrectly on systems using single pass stereo with the XR plugin system - e.g. Windows Mixed Reality headsets. SteamVR headsets may also default to single pass stereo, showing the same issue. However in this case, the OpenVR settings can be changed to multipass which resolves the problem.
-	Demo scenes do not start at the correct height for a seated user. The XR Plugin Management System adjusts the camera height. This means the user has to adjust components in the scene to the correct height - e.g. camera height. Currently our position is to support the legacy XR system height settings.
-	Capsule hands appear small compared to size of 'IR hands' of user using HDRP / URP and do not line up. Using standard rendering on Unity 2019.4 LTS  hands are usually not visible (but are being tracked). When they appear they do not line up with the hands in the image.
- Possible hand offset issues on XR2 headsets using SVR plugin
- Hands in Desktop scenes can appear far away from the camera
- Interactions callback scene allows blocks to be moved without doing a grasp pose.
- Interactions object scene platform/stage seems to move a lot
- Dynamic UI objects throwing backwards most of the time.


## [5.3.0] 

### Added

### Changed
- Clear devices list on disconnect of service Connection.cs
- Example scenes now contain a clickable link to take users to https://docs.ultraleap.com/ultralab/
- Removed unused variables from Connection and Controller
- Hand Model Base feature parity with the interaction hand
- LeapXRServiceProvider getter and setter for MainCamera

### Removed

### Fixed
- Outline/Ghost hands sometimes show a shader issue when upgrading to SRP (TOON shader)
- Jittery Sliders and slider problems in moving reference frame
- When using LeapXRServiceProvider with Temporal Warping enabled, the hands fly off in the first few frames.
- Reduced the number of OnContactBegin / OnContactEnd events when a finger is in contact with a slider
- Fixed issues with HDRP and URP example scenes not containing the correct shader when switching graphics pipelines.
- Fixing eye dislocator misalignment
- Unused variables in LeapCSharp Controller and Connection causing warnings [[#1181]](https://github.com/ultraleap/UnityPlugin/issues/1181)

### Known issues
-	Scenes containing the infrared viewer render incorrectly on systems using single pass stereo with the XR plugin system - e.g. Windows Mixed Reality headsets. SteamVR headsets may also default to single pass stereo, showing the same issue. However in this case, the OpenVR settings can be changed to multipass which resolves the problem.
-	Demo scenes do not start at the correct height for a seated user. The XR Plugin Management System adjusts the camera height. This means the user has to adjust components in the scene to the correct height - e.g. camera height. Currently our position is to support the legacy XR system height settings.
-	Capsule hands appear small compared to size of 'IR hands' of user using HDRP / URP and do not line up. Using standard rendering on Unity 2019.4 LTS  hands are usually not visible (but are being tracked). When they appear they do not line up with the hands in the image.
- Possible hand offset issues on XR2 headsets using SVR plugin
- Hands in Desktop scenes can appear far away from the camera
- Interactions callback scene allows blocks to be moved without doing a grasp pose.
- Interactions object scene platform/stage seems to move a lot
- Dynamic UI objects throwing backwards most of the time.
- Service Providers not referenced in Hand Post-Process example scene (to fix: drag 'Intertia Hand Models' into the leap Provider of its children capsule hands)


## [5.2.0]

### Added
- Adding SIR170 leapc/device.
- Adding 3DI leapc/device
- Adding option to grasp interaction objects with a specific hand


### Changed

- Moved SimpleFacingCameraCallbacks.cs to Interaction Engine\Runtime\Scripts\Utility & updated its namespace
- Update main camera provider to enable work on supporting MRTK

### Removed

### Fixed
- https://github.com/ultraleap/UnityPlugin/issues/1177

### Known issues
-	Scenes containing the infrared viewer render incorrectly on systems using single pass stereo with the XR plugin system - e.g. Windows Mixed Reality headsets. SteamVR headsets may also default to single pass stereo, showing the same issue. However in this case, the OpenVR settings can be changed to multipass which resolves the problem.
-	Demo scenes do not start at the correct height for a seated user. The XR Plugin Management System adjusts the camera height. This means the user has to adjust components in the scene to the correct height - e.g. camera height. Currently our position is to support the legacy XR system height settings.
-	Capsule hands appear small compared to size of 'IR hands' of user using HDRP / URP and do not line up. Using standard rendering on Unity 2019.4 LTS  hands are usually not visible (but are being tracked). When they appear they do not line up with the hands in the image.
- Outline/Ghost hands sometimes show a shader issue when upgrading to SRP (TOON shader)
- Issues with slider button movements not being possible or registering false presses in moving reference frames scene when frame is moving (inconsistent). Only affects slider buttons - normal buttons work fine.
- Possible hand offset issues on XR2 headsets using SVR plugin
- Hands in Desktop scenes can appear far away from the camera
- Interactions callback scene allows blocks to be moved without doing a grasp pose.
- Interactions object scene platform/stage seems to move a lot
- Dynamic UI objects throwing backwards most of the time.

## [5.1.0]

### Added
- Adding coloring options to the capsule hands
- New option to initialise only the index finger in the interaction hand

### Changed
- Size of the Skeleton hand assets has been significantly reduced

### Removed

### Fixed
- Generic Hand Model rendering issue with transparency
- Updated XR2 preview documentation ('How to Build a Unity Application that Shows Tracked Hands on an XR2') to account for asset path changes, name changes to preview packages in V5.0.0 (from expermimental) and in response to internal user testing
- Minor changes to anchors example scene

### Known issues
-	Scenes containing the infrared viewer render incorrectly on systems using single pass stereo with the XR plugin system - e.g. Windows Mixed Reality headsets. SteamVR headsets may also default to single pass stereo, showing the same issue. However in this case, the OpenVR settings can be changed to multipass which resolves the problem.
-	Demo scenes do not start at the correct height for a seated user. The XR Plugin Management System adjusts the camera height. This means the user has to adjust components in the scene to the correct height - e.g. camera height. Currently our position is to support the legacy XR system height settings.
-	Capsule hands appear small compared to size of 'IR hands' of user using HDRP / URP and do not line up. Using standard rendering on Unity 2019.4 LTS  hands are usually not visible (but are being tracked). When they appear they do not line up with the hands in the image.
- Outline/Ghost hands sometimes show a shader issue when upgrading to SRP (TOON shader)
- Issues with slider button movements not being possible or registering false presses in moving reference frames scene when frame is moving (inconsistent). Only affects slider buttons - normal buttons work fine.
- Possible hand offset issues on XR2 headsets using SVR plugin


## [5.0.0]
### Added
- Support for Unity HDRP and URP including materials and shaders in all examples
- Hands module shaders for outline, ghost and skeleton hands
- `Service Provider` (XR, Desktop and Screentop) prefabs
- `Image Retriever` prefab
- `HandModels` prefab
- Experimental support for Qualcomm Snapdragon XR2 based headsets within `com.ultraleap.tracking.preview` package.
- MainCameraProvider.cs to get the camera on Android platforms

### Changed
- Reorganized the repository layout to adhere to [UPM Package Structure](https://docs.unity3d.com/Manual/cus-layout.html). Fixes [[#1113]](https://github.com/ultraleap/UnityPlugin/issues/1113)
  - Core, Hands and Interaction Engine modules are in their own sub-folders with Editor/Runtime folders in a `com.ultraleap.tracking` UPM package.
  - Examples for all modules are in hidden `Examples~` folders within their respective package. These can be imported as samples from the package manager window or unhidden by removing the `~` when importing from .unitypackages.
  - UIInput module has is now in a separate preview package "com.ultraleap.tracking.preview".
- The following scripts are no longer required to be put on a `Camera`. Instead, they require a reference to a `Camera`.
  - LeapXRServiceProvider
  - LeapImageRetriever
  - LeapEyeDislocator
  - EnableDepthBuffer
- Reworked how adding hands to a scene works - hands can be added easily. Any type derived from `HandModelBase` can be added directly into the scene and linked with a `LeapProvider` to begin tracking immediately.
- `Frame.Get` renamed to `Frame.GetHandWithChirality`.
- Rebranded Leap Motion Unity Modules Window


### Removed
- `HandModelManager` MonoBehaviour
- `Leap Rig` Prefab
- `Leap Hand Controller` Prefab
- The following example scenes were removed:
  - Rigged Hands (VR - Infrared Viewer)
  - Rigged Hands (VR)
- Experimental modules
  - HierarchyRecording
  - Playback
- Docs - migrated to [Ultraleap Docs][docs-website]
- Internal directory
  - AutoHeader (Moved to CI folder, no longer part of any packages)
  - Generation
  - RealtimeGraph
  - Testing
  - VRVisualizer
- Legacy directory
  - DetectionExamples
  - GraphicRenderer

### Fixed
- Missing rigged hands image (Note that docs moved to [Ultraleap Docs](https://docs.ultraleap.com/unity-api/unity-user-manual/core.html)) [[#1172]](https://github.com/ultraleap/UnityPlugin/issues/1172)
- 'SelectionMode.OnlyUserModifiable' is obsolete [[1167]](https://github.com/ultraleap/UnityPlugin/issues/1167)
- Initializing contact bones of XR controller [[#1085]](https://github.com/ultraleap/UnityPlugin/issues/1085)
- enableContactBoneCollision() called unnecessarily often [[#1062]](https://github.com/ultraleap/UnityPlugin/issues/1062)
- ClearContactTracking() doesn't clear a pooled Hashset before calling Recycle() [[#1061]](https://github.com/ultraleap/UnityPlugin/issues/1061)
- Hand position jumps when using OVRProvider [[#1054]](https://github.com/ultraleap/UnityPlugin/issues/1054) 
- Remove additional audio listeners in example scenes
- Clipping plane in example scenes is not set close enough, Hands models are being clipped
- Images not seen in Core examples - Capsule hands (VR - Infrared Viewer)

### Known issues
-	Scenes containing the infrared viewer render incorrectly on systems using single pass stereo with the XR plugin system - e.g. Windows Mixed Reality headsets. SteamVR headsets may also default to single pass stereo, showing the same issue. However in this case, the OpenVR settings can be changed to multipass which resolves the problem.
-	Demo scenes do not start at the correct height for a seated user. The XR Plugin Management System adjusts the camera height. This means the user has to adjust components in the scene to the correct height - e.g. camera height. Currently our position is to support the legacy XR system height settings.
-	Capsule hands appear small compared to size of 'IR hands' of user using HDRP / URP and do not line up. Using standard rendering on Unity 2019.4 LTS  hands are usually not visible (but are being tracked). When they appear they do not line up with the hands in the image.
- Outline/Ghost hands sometimes show a shader issue when upgrading to SRP (TOON shader)
- Issues with slider button movements not being possible or registering false presses in moving reference frames scene when frame is moving (inconsistent). Only affects slider buttons - normal buttons work fine.
- Possible hand offset issues on XR2 headsets using SVR plugin

## [4.9.1 and older]

[older-releases]: https://github.com/ultraleap/UnityPlugin/releases "UnityPlugin Releases"

Refer to the [release notes page][older-releases] for older releases.
