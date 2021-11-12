# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [5.0.0] - Release date TBC
### Added
- Support for Unity HDRP and URP
  - Materials and shaders in all examples
  - Hands module shaders for outline, ghost and skeleton hands
- `Service Provider` (XR, Desktop and Screentop) prefabs
- `Image Retriever` prefab
- `HandModels` prefab
- Experimental support for Qualcomm Snapdragon XR2 based headsets

### Changed
- Reorganized the repository layout to adhere to [UPM Package Structure](https://docs.unity3d.com/Manual/cus-layout.html)
  - Core, Hands and Interaction Engine modules are in their own sub-folders with Editor/Runtime folders
  - UIInput module has is now an experimental module and is available to import as a sample from the package manager window
  - Examples for all modules have moved to the `Examples~` folder and are importable from the package manager window
- The following scripts are no longer required to be put on a `Camera`. Instead, they require a reference to a `Camera`.
  - LeapXRServiceProvider
  - LeapImageRetriever
  - LeapEyeDislocator
  - EnableDepthBuffer
- Reworked how adding hands to a scene works - hands can be added easily. Any type derived from `HandModelBase` can be added directly into the scene and linked with a `LeapProvider` to begin tracking immediately.
- Frame.Get to Frame.GetHandWithChirality.

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

## [4.9.1 and older]

[older-releases]: https://github.com/leapmotion/UnityModules/releases "UnityModules Releases"

Refer to the [release notes page][older-releases] for Unity Modules repository.
