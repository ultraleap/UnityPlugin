# UHI Tracking

[[_TOC_]]

## Introduction

This repository hosts the complete Ultraleap UHI Unity Tracking plugin and developer SDK.


## Requirements

You will need a Leap Motion compatible device to use this plugin.

You will need to have the Leap Motion service installed, which must be downloaded separately:
  https://developer.leapmotion.com/


## Getting Started

Please look at any of the example scenes in the Examples subdirectory as a starting point for how to use this plugin.


## Updating Previous Installations

Due to the Unity package importer being strictly additive and not handling file deletions, renames, or moves well; we recommend you fully delete your Assets/Plugins/LeapMotion/Core folder before importing this updated package into your project. This will also be a  requirement for future projects.

We recommend that any new prefabs or scripts you write be stored outside Leap Motion folders to make these updates easier.


## Questions?

Please feel free to reach out with any questions, bugs or feature requests on the LeapMotion developer forums: https://community.leapmotion.com/c/development