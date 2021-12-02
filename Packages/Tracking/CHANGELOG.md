# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [5.0.0] - Release date 8th December 2021 (Subject to change)
### Added
- Support for Unity HDRP and URP
- Materials and shaders in all examples
- Hands module shaders for outline, ghost and skeleton hands
- `Service Provider` (XR, Desktop and Screentop) prefabs
- `Image Retriever` prefab
- `HandModels` prefab
- Experimental support for Qualcomm Snapdragon XR2 based headsets
- MainCameraProvider.cs to get the camera on Android platforms

### Changed
- Reorganized the repository layout to adhere to [UPM Package Structure](https://docs.unity3d.com/Manual/cus-layout.html)
  - Core, Hands and Interaction Engine modules are in their own sub-folders with Editor/Runtime folders
  - Examples for all modules have moved to a separate `Examples` package `com.ultraleap.tracking.examples`.
  - UIInput module has is now a separate experimental package "com.ultraleap.tracking.ui-input".
- The following scripts are no longer required to be put on a `Camera`. Instead, they require a reference to a `Camera`.
  - LeapXRServiceProvider
  - LeapImageRetriever
  - LeapEyeDislocator
  - EnableDepthBuffer
- Reworked how adding hands to a scene works - hands can be added easily. Any type derived from `HandModelBase` can be added directly into the scene and linked with a `LeapProvider` to begin tracking immediately.
- Frame.Get to Frame.GetHandWithChirality.
- Leap Motion Unity Modules Window


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
- Docs - migrated elsewhere
- Internal directory
  - AutoHeader
  - Generation
  - RealtimeGraph
  - Testing
  - VRVisualizer
- Legacy directory
  - DetectionExamples
  - GraphicRenderer

### Fixed
- Hand position jumps when using OVRProvider [[UnityModules#1054]](https://github.com/leapmotion/UnityModules/issues/1054) 
- Initializing contact bones of XR controller 
- enableContactBoneCollision() called unnecessarily often 
- ClearContactTracking() doesn't clear a pooled Hashset before calling Recycle()
- Remove additional audio listeners in example scenes
- Clipping plane in example scenes is not set close enough, Hands models are being clipped
- Images not seen in Core examples - Capsule hands (VR - Infrared Viewer)

### Known issues
-	Scenes containing the infrared viewer render incorrectly on systems using single pass stereo with the XR plugin system - e.g. Windows Mixed Reality headsets. SteamVR headsets may also default to single pass stereo, showing the same issue. However in this case, the OpenVR settings can be changed to multipass which resolves the problem.
-	Demo scenes do not start at the correct height for a seated user. The XR Plugin Management System adjusts the camera height. This means the user has to adjust components in the scene to the correct height. Currently our position is to support the legacy XR system height settings.
-	Capsule hands appear small compared to size of 'IR hands' of user using HDRP and do not line up.
- Outline/Ghost hands sometimes show a shader issue when upgrading to SRP (TOON shader)

## [4.9.1 and older]

[older-releases]: https://github.com/leapmotion/UnityModules/releases "UnityModules Releases"

Refer to the [release notes page][older-releases] for Unity Modules repository.
