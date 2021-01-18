# Leap Motion Unity Modules

To download Leap Motion's latest stable modules as .unitypackages, visit [our Unity developer site][devsite]. Be sure to also check out our [documentation and API reference][um-docs]!

The UnityModules repository is designed to be opened in 2019.2. Our release packages should support 5.6.2 and up, for stable Unity releases. If you hit an issue with version support, please submit a ticket with any details!

Since Unity Modules 4.5.0, our [releases page][releases] on this repository is the official source for distribution. This version of UnityModules fixes compilation warnings in Unity 2019 and includes some fixes for newer Unity features like the Scriptable Render Pipeline (in 2019.1 and beyond).

### Notes on SpatialTracking import errors.

* Unity 2019.4 users: If you get errors related to "SpatialTracking" upon importing the Core module, you will need to install [XR Legacy Input Helpers](http://docs.unity3d.com/Packages/com.unity.xr.legacyinputhelpers@2.1/manual/index.html).

* Unity 2020.1 users: If you get errors related to "SpatialTracking" upon importing the Core module, you will need to install the [Oculus XR package](http://docs.unity3d.com/Packages/com.unity.xr.oculus@1.4/manual/index.html).

### Notes on Unity 2018.4 (LTS).

* It is not possible to support Unity 2018 LTS -> Unity 2020.2 in a single package. This release is optimised for Unity 2019.4. However, it is possible to use it in Unity 2018.4.
* To do this delete the manifest.json file in the packages folder. Also delete the LeapMotion.asmdef file inside Assets/Plugins/LeapMotion/Core/Scripts and then open the project. Note, a couple of the scenes will not work in this version of Unity. These are FingerDetectionDemo and VRVisualizer.

## License

Use of Leap Motion's UnityModules is subject to the [Apache V2 License Agreement][apache].

## This repository

This repository contains code for Leap Motion's Unity Modules, easy-to-use tools for integrating the Leap Motion Controller in Unity projects, and various utilities for VR and AR projects.

**Note that this repository also contains code for work-in-progress modules, tentative modules, or older modules that may be unsupported.** We recommend using the packages available on the [developer site][devsite] unless you're planning on contributing or you are otherwise feeling *particularly hardcore*.

[um-docs]: https://leapmotion.github.io/UnityModules/
[devsite]: https://developer.leapmotion.com/unity/ "Leap Motion Unity Developer site"
[wiki]: https://github.com/leapmotion/UnityModules/wiki "Leap Motion Unity Modules Wiki"
[apache]: http://www.apache.org/licenses/LICENSE-2.0 "Apache V2 License"
[releases]: https://github.com/leapmotion/UnityModules/releases
