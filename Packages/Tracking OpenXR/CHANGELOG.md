# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

[docs-website]: https://docs.ultraleap.com/ "Ultraleap Docs"

## [1.0.0-pre.2] - 15/03/2022

### Fixed

- Fixed a marshalling issue that resulted in crashes when errors during creation of OpenXR hand trackers.

## [1.0.0-pre.1]

### Added

- Added an OpenXR validation check/auto-fix for the main camera near clipping plain distance, to ensure tracked hands are not clipped at normal hand interaction distances.

### Fixed

- Fixed Android missing from the list of supported targets for the Runtime package.
- Added example scene showing Capsule Hands used with a `Service Provider (OpenXR)` prefab.

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
