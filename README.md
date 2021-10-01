<!--links-->
[upgrade-urp]: https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@7.1/manual/upgrading-your-shaders.html "Unity URP Upgrade Documentation"
[upgrade-hdrp]: https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@7.1/manual/Upgrading-To-HDRP.html "Unity HDRP Upgrade Documentation"
[upgrade-xr]: https://docs.unity3d.com/Manual/configuring-project-for-xr.html "Unity XR Upgrade Documentation"
[package-manager]: https://docs.unity3d.com/Manual/Packages.html "Unity Package Manager Documentation"
[apache]: http://www.apache.org/licenses/LICENSE-2.0 "Apache V2 License"

[documentation]: https://docs.ultraleap.com/ "Ultraleap UnityPlugin Documentation"
[api-reference]: https://docs.ultraleap.com/ "Ultraleap UnityPlugin API Reference"
[developer-site]: https://developer.leapmotion.com/ "Ultraleap Developer Site"
[developer-site-unity]: https://developer.leapmotion.com/unity/ "Ultraleap Developer site - Unity"
[releases]: https://github.com/ultraleap/UnityPlugin/releases "UnityPlugin releases"
[developer-forum]: https://forums.leapmotion.com/ "Developer Forum"
[repository-clone-url]: https://github.com/ultraleap/UnityPlugin.git "Clone with HTTPS"




<!--content-->
# Ultraleap Unity Plugin

## Introduction

Ultraleap's Unity Plugin enables the data produced by Ultraleap tracking hardware to be used by developers inside their Unity projects. It includes various utilities, examples and prefabs that make it as easy as possible to design and use hand tracking in XR projects

This repository hosts the complete Ultraleap Unity Plugin and Ultraleap Tracking SDK. Examples are included to quickly get you up and running with Ultraleap's hand tracking technology.

## Requirements

The UnityPlugin repository is designed to work with Unity 2019.4 LTS or newer. However, due to the ever changing landscape of package dependencies in Unity we cannot guarantee compatibility with every plugin or variant of Unity, but aim to provide support for all LTS versions that are under continuous support from Unity. If you are sourcing UnityPlugin directly from this repository, your mileage may vary with earlier versions of Unity.

You will need an Ultraleap compatible tracking device to use this plugin. In addition you will need to have the Ultraleap Gemini tracking service installed, which must be [downloaded separately][developer-site].

Our content uses the standard shader throughout the majority of our examples. However, it is possible to view these example scenes according to whichever pipeline you choose by using the upgrading functionality for render pipelines, supplied by Unity.
To ensure you can continue using our hand models and custom shaders, we also supply an extra URP & HDRP compatible example scene. __(Note these will only work with the correct SRP installed)__

Use of Ultraleap's UnityPlugin is subject to the [Apache V2 License Agreement][apache].

## Dependencies

If you are using Unity 2020.4 (LTS) or newer with XR then you will need to follow the Unity documentation on how to configure your project.
  * [Upgrading to XR plugin Management System][upgrade-xr]

If you are using any of the scriptable render pipelines (SRP) then you will need to follow the appropriate Unity documentation for upgrading shaders:
* [Upgrading to Universal Render Pipeline (URP)][upgrade-urp] 
* [Upgrading to High DefinitionRender Pipeline (URP)][upgrade-hdrp]




# Getting Started

This plugin has been configured to be used with the [Unity Package Manager (UPM)][package-manager], but we also provide a \*.unitypackage for those who wish to follow the legacy approach which can be obtained from [our developer site][developer-site-unity] or from the [release section][releases].

Be sure to also check out our [documentation][documentation].

## Installation

1. __UPM via GitHub__  
  To add a (read-only) UPM package remotely via a GitHub URL select the option "Add package from git URL…" in the Unity package manager. Paste the link to [clone the repository][repository-clone-url] into the input field that appears and press enter. The package will then be added to your project and you should be good to go! 
    * *Requires Unity 2019.4 (LTS) or newer.*

2. __UPM Local Package__  
  To add a (read-only) UPM package locally from a cloned repository select the option "Add package from disk…" and point it at the root folder of the cloned repository on your file system.
    * *By default this will use an absolute file path from your machine, so will not be a sharable solution without some modification.*

3. __UPM Embedded Package__  
  To add a (editable) UPM package locally from a cloned repository place it within the Packages folder of your Unity project.
    * *This is perhaps the easiest way to work if you want o submit a pull request against the Ultraleap Unity Plugin.*

4. __Unity Package__  
  Import the package (e.g. Assets -> Import Package -> Custom Package...) which can be downloaded from [our Unity developer site][developer-site-unity] or the [releases section][releases] of this repository.     
    * *\*.unitypackage(s) are a deprecated solution in Unity. Do not move the location of the installed plugin as this may break certain features.*

5. __Submodule__  
  You can also add this plugin as a submodule in your assets folder. 
    * *Use this method with caution as submodules can introduce their own complexities to a project*

<!--6. OpenUPM
Add the following scoped registry to Unity (Edit -> Project Settings... -> Package Manager -> Scoped Registries) 
  Name: Ultraleap - OpenUPM
  URL: https://package.openupm.com
  Scope(s): com.ultraleap
  Then select "My Registries" from the package manager and install the Ultraleap Unity plugin.-->


## Upgrading Existing Installation

## Quick-Start Guide

## Feedback

For any further questions or feedback please visit our [developer forums][developer-forum].
For any issues encountered when using the plugin, please raise an issue on GitHub. ALternatively, if you have a fix, please submit a merge request for us to review.
