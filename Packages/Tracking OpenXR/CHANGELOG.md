# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

[docs-website]: https://docs.ultraleap.com/ "Ultraleap Docs"

## [NEXT] - unreleased

### Added

- 

### Known issues

- Currently the Ultraleap Hand Tracking feature for OpenXR requires the New and Legacy input systems to be enabled, to simultaneously use OpenXR and the Ultraleap Unity Plugin's features.
- The OpenXR Leap Provider does not currently support the following hand properties (and will return fixed values):
  - Hand and Finger: `FrameId`
  - Hand: `Confidence`
  - Hand: `GrabAngle`
  - Hand and Finger: `Id` & `HandId` will always return `0` and `1` for the left and right hand respectively
  - Hand and Finger: `TimeVisible`
  - Finger: `IsExtended`

## [1.0.0-pre.5] - 23/06/2022

### Added

- Added support for `XR_ULTRALEAP_hand_tracking_elbow` which provides elbow tracking if supported on the system.

### Known issues

- Currently the Ultraleap Hand Tracking feature for OpenXR requires the New and Legacy input systems to be enabled, to simultaneously use OpenXR and the Ultraleap Unity Plugin's features.
- The OpenXR Leap Provider does not currently support the following hand properties (and will return fixed values):
  - Hand and Finger: `FrameId`
  - Hand: `Confidence`
  - Hand: `GrabAngle`
  - Hand and Finger: `Id` & `HandId` will always return `0` and `1` for the left and right hand respectively
  - Hand and Finger: `TimeVisible`
  - Finger: `IsExtended`

## [1.0.0-pre.4] - 10/06/2022

### Fixed

- Fixed Hand data provided via the OpenXR Leap Provider was always relative to the scene origin. It now respects any parent transforms applied to the main camera and works correctly with `Tracked Pose Driver`.

### Known issues

- Currently the Ultraleap Hand Tracking feature for OpenXR requires the New and Legacy input systems to be enabled, to simultaneously use OpenXR and the Ultraleap Unity Plugin's features.
- The OpenXR Leap Provider does not currently support the following hand properties (and will return fixed values):
  - Hand and Finger: `FrameId`
  - Hand: `Confidence`
  - Hand: `GrabAngle`
  - Hand and Finger: `Id` & `HandId` will always return `0` and `1` for the left and right hand respectively
  - Hand and Finger: `TimeVisible`
  - Finger: `IsExtended`
- The elbow joint is not currently tracked and the OpenXR Leap Provider infers the elbow/forearm from the hand-data.


## [1.0.0-pre.3] - 19/04/2022

### Added

- Added support for `PinchStrength`, `PinchDistance` and `PalmWidth` on hands returned by the OpenXR Provider.

### Changed

- The OpenXR service provider now populates frames in Update() and reuses the update frame in FixedUpdate() (similar to '_Reuse Update for Physics_' option other service providers)

### Fixed

- Issues with Capsule Hands rendering in both the scene and the game view if both tabs are open

### Known issues

- Currently the Ultraleap Hand Tracking feature for OpenXR requires the New and Legacy input systems to be enabled, to simultaneously use OpenXR and the Ultraleap Unity Plugin's features.
- The OpenXR Leap Provider does not currently support the following hand properties (and will return fixed values):
  - Hand and Finger: `FrameId`
  - Hand: `Confidence`
  - Hand: `GrabAngle`
  - Hand and Finger: `Id` & `HandId` will always return `0` and `1` for the left and right hand respectively
  - Hand and Finger: `TimeVisible`
  - Finger: `IsExtended`
- The elbow joint is not currently tracked and the OpenXR Leap Provider infers the elbow/forearm from the hand-data.

## [1.0.0-pre.2] - 15/03/2022

### Fixed

- Fixed a marshalling issue that resulted in crashes when errors during creation of OpenXR hand trackers.

### Known issues

- Hand data provided via the OpenXR Leap Provider is currently always relative to the scene origin. It will not respect any parent transforms applied to the main camera.
- Currently the Ultraleap Hand Tracking feature for OpenXR requires the New and Legacy input systems to be enabled, to simultaneously use OpenXR and the Ultraleap Unity Plugin's features.
- The OpenXR Leap Provider does not currently support the following hand properties (and will return fixed values):
  - Hand and Finger: `FrameId`
  - Hand: `Confidence`
  - Hand: `GrabAngle`
  - Hand: `PinchStrength`
  - Hand: `PinchDistance`
  - Hand: `PalmWidth`
  - Hand and Finger: `Id` & `HandId` will always return `0` and `1` for the left and right hand respectively
  - Hand and Finger: `TimeVisible`
  - Finger: `IsExtended`
- The elbow joint is not currently tracked and the OpenXR Leap Provider infers the elbow/forearm from the hand-data.

## [1.0.0-pre.1]

### Added

- Added an OpenXR validation check/auto-fix for the main camera near clipping plain distance, to ensure tracked hands are not clipped at normal hand interaction distances.

### Fixed

- Fixed Android missing from the list of supported targets for the Runtime package.
- Added example scene showing Capsule Hands used with a `Service Provider (OpenXR)` prefab.
- 
### Known issues

- Hand data provided via the OpenXR Leap Provider is currently always relative to the scene origin. It will not respect any parent transforms applied to the main camera.
- Currently the Ultraleap Hand Tracking feature for OpenXR requires the New and Legacy input systems to be enabled, to simultaneously use OpenXR and the Ultraleap Unity Plugin's features.
- The OpenXR Leap Provider does not currently support the following hand properties (and will return fixed values):
  - Hand and Finger: `FrameId`
  - Hand: `Confidence`
  - Hand: `GrabAngle`
  - Hand: `PinchStrength`
  - Hand: `PinchDistance`
  - Hand: `PalmWidth`
  - Hand and Finger: `Id` & `HandId` will always return `0` and `1` for the left and right hand respectively
  - Hand and Finger: `TimeVisible`
  - Finger: `IsExtended`
- The elbow joint is not currently tracked and the OpenXR Leap Provider infers the elbow/forearm from the hand-data.

## [1.0.0-pre.0]

### Added

- Ultraleap Hand Tracking Feature for OpenXR. This feature enables hand-tracking using the XR_EXT_hand_tracking extension.
- OpenXR Leap Provider which allows using OpenXR hand-tracking with the Ultraleap Unity Plugin.
- A simple Hand Joint Visulizer which displays the joints of the hands from the OpenXR feature using sphere primatives.

### Known issues

- Hand data provided via the OpenXR Leap Provider is currently always relative to the scene origin. It will not respect any parent transforms applied to the main camera.
- Currently the Ultraleap Hand Tracking feature for OpenXR requires the New and Legacy input systems to be enabled, to simultaneously use OpenXR and the Ultraleap Unity Plugin's features.
- The OpenXR Leap Provider does not currently support the following hand properties (and will return fixed values):
  - Hand and Finger: `FrameId`
  - Hand: `Confidence`
  - Hand: `GrabAngle`
  - Hand: `PinchStrength`
  - Hand: `PinchDistance`
  - Hand: `PalmWidth`
  - Hand and Finger: `Id` & `HandId` will always return `0` and `1` for the left and right hand respectively
  - Hand and Finger: `TimeVisible`
  - Finger: `IsExtended`
- The elbow joint is not currently tracked and the OpenXR Leap Provider infers the elbow/forearm from the hand-data.
