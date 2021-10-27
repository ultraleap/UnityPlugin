# Ultraleap Unity Modules

To download Ultraleap's latest stable modules as .unitypackages, visit [our Unity developer site][devsite]. Be sure to also check out our [documentation and API reference][um-docs]!

**Notable changes:**

- Version 4.9.1 of the Unity Modules is only intended to be used with Version 5.2 of the tracking service (or more recent releases), it is not backwards compatible with previous service releases. For details about service compatibility with Unity Modules and how to upgrade projects, consult the [Migration Guide](https://developer.leapmotion.com/migration-guide).


- 32-bit support has been dropped as it is no longer supported by the Gen 5.2 tracking service

- To use V5.2 you must upgrade your project to this version of the Unity modules.

- The documentation and API reference has been upgraded for branding and to reflect changes to the modules.

- A new tracking mode setting has been added to the LeapServiceProvider to disable setting a tracking mode on start. If set, the (current) service tracking mode is left unchanged by the application when it connects to the service.

- The scripts have been changed to switch to using the Unity pose class, retiring the Leap pose class. A number of extension methods have been added to the Unity pose class to create a similar public API to the retired class.

The UnityModules repository is designed to be opened in 2019.2. Our release packages should support 5.6.2 and up, for stable Unity releases. However, this release has been tested with Unity 2019.4 LTS and Unity 2020.2. There is a separate release that supports Unity 2018.4 LTS. If you hit an issue with version support, please submit a ticket with any details!

Since Unity Modules 4.5.0, our [releases page][releases] on this repository is the official source for distribution. This version of UnityModules fixes compilation warnings in Unity 2019 and includes some fixes for newer Unity features like the Scriptable Render Pipeline (in 2019.1 and beyond).

### Notes on SpatialTracking import errors.

* Unity 2019.4 users: If you get errors related to "SpatialTracking" upon importing the Core module, you will need to install [XR Legacy Input Helpers](http://docs.unity3d.com/Packages/com.unity.xr.legacyinputhelpers@2.1/manual/index.html).

* Unity 2020.1 users: If you get errors related to "SpatialTracking" upon importing the Core module, you will need to install the [Oculus XR package](http://docs.unity3d.com/Packages/com.unity.xr.oculus@1.4/manual/index.html).

## License

Use of Ultraleap's UnityModules is subject to the [Apache V2 License Agreement][apache].

## This repository

This repository contains code for Ultraleap's Unity Modules, easy-to-use tools for integrating the Ultraleap camera hardware in Unity projects, and various utilities for VR and AR projects.

**Note that this repository also contains code for work-in-progress modules, tentative modules, or older modules that may be unsupported.** We recommend using the packages available on the [developer site][devsite] unless you're planning on contributing or you are otherwise feeling *particularly hardcore*.

[um-docs]: https://leapmotion.github.io/UnityModules/
[devsite]: https://developer.leapmotion.com/unity/ "Ultraleap for Developers site"
[wiki]: https://github.com/leapmotion/UnityModules/wiki "Ultraleap / Leap Motion Unity Modules Wiki"
[apache]: http://www.apache.org/licenses/LICENSE-2.0 "Apache V2 License"
[releases]: https://github.com/leapmotion/UnityModules/releases
