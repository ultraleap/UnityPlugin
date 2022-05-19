# Ultraleap OVRLeapProvider

Update of [[DRAFT] OVRProvider - Oculus Quest Hand Tracking](https://github.com/ultraleap/UnityPlugin/pull/1166), based on the latest develop branch.

This is an experimental compatibility layer that allows to use Quest hand tracking with interactions built using the Leap Motion Unity Modules.

## Setup

1. Download the [Oculus Integration](https://assetstore.unity.com/packages/tools/integration/oculus-integration-82022) from the Asset Store
1. Create a new Unity Project
1. Follow [these instructions](https://developer.oculus.com/documentation/unity/unity-handtracking/) provided by Oculus to set up hand tracking in Unity
1. Copy contents of `./copythis~` to `Assets/Oculus/SampleFramework`
3. Import packages and examples from disk:
	- `Packages/Tracking`
	- `Packages/OVR Leap Provider` 
4. Open up `Assets\Samples\Ultraleap OVR Leap Provider\5.8.0\Examples\Interaction Objects.unity`