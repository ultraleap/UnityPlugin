# Ultraleap OVRLeapProvider

This is an update of [[DRAFT] OVRProvider - Oculus Quest Hand Tracking](https://github.com/ultraleap/UnityPlugin/pull/1166), based on the latest develop branch.

This is an experimental compatibility layer that allows to use Quest hand tracking with interactions built using the Leap Motion Unity Modules.

## Setup

1. Download the [Oculus Integration](https://assetstore.unity.com/packages/tools/integration/oculus-integration-82022) from the Asset Store
1. Create a new Unity Project
1. Follow [these instructions](https://developer.oculus.com/documentation/unity/unity-handtracking/) provided by Oculus to set up hand tracking in Unity
1. Import packages from disk:
	- dsd
2. Fix Oculus-Leap circular dependency by adding an asmdef for the sample framework:
  	- Copy the following:
  	- src: `./ASMDEF - Oculus Sample Framework/oculus.sampleframework.asmdef` 
  	- dest: `Assets/Oculus/SampleFramework`

## Requirements

### Oculus Integration

* 

### Setup

* Import the Unity Modules
* 
* Open one of the example scenes included with the Unity Modules and replace the traditional LeapServiceProvider/LeapXRServiceProvider with an OVRProvider
* Assign the OVRProvider as to components that reference a LeapProvider (e.g. InteractioHands and HandModelManager)