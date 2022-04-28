# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

[docs-website]: https://docs.ultraleap.com/ "Ultraleap Docs"

## [5.8.0]

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
