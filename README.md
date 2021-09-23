<!--links-->
[upgrade-urp]: https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@7.1/manual/upgrading-your-shaders.html "Unity URP Upgrade Documentation"
[upgrade-hdrp]: https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@7.1/manual/Upgrading-To-HDRP.html "Unity HDRP Upgrade Documentation"
[upgrade-xr]: https://docs.unity3d.com/Manual/configuring-project-for-xr.html "Unity XR Upgrade Documentation"
[package-manager]: https://docs.unity3d.com/Manual/Packages.html "Unity Package Manager Documentation"
[apache]: http://www.apache.org/licenses/LICENSE-2.0 "Apache V2 License"

[documentation]: https://docs.ultraleap.com/ "Ultraleap UnityPlugin Documentation"
[api-reference]: https://docs.ultraleap.com/ "Ultraleap UnityPlugin API Reference"
[devsite]: https://developer.leapmotion.com/ "Ultraleap Developer Site"
[devsite-unity]: https://developer.leapmotion.com/unity/ "Ultraleap Developer site - Unity"
[releases]: https://github.com/leapmotion/UnityModules/releases "UnityPlugin releases"




<!--TODO-->
# Prior to release
## Update Content

- [x] Initial documentation pass
- [ ] Update introduction
- [ ] Update upgrade existing modules
- [ ] Add quick start guide
- [ ] Determine if there is any missing content
- [ ] Add any missing content

## Update Links
- [ ] Update [documentation] link
- [ ] Update [api-reference] link
- [ ] Update [devsite] link
- [ ] Update [devsite-unity] link
- [ ] Update [releases] link




<!--content-->
# Ultraleap Unity Plugin

## Introduction

This repository hosts the complete Ultraleap Unity Plugin and developer SDK. Examples are included to quickly get you up and running with Ultraleap's hand tracking technology.

## Requirements

You will need a Leap Motion compatible device to use this plugin. In addition you will need to have the Leap Motion service installed, which must be [downloaded separately][devsite]:

Use of Ultraleap's UnityPlugin is subject to the [Apache V2 License Agreement][apache].

## Dependencies

If you are using Unity 2020.4 (LTS) or newer with XR then you will need to follow the Unity documentation on how to configure your project.
  * [Upgrading to XR plugin Management System][upgrade-xr]

If you are using any of the scriptable render pipelines (SRP) then you will need to follow the appropriate Unity documentation for upgrading shaders:
* [Upgrading to Universal Render Pipeline (URP)][upgrade-urp] 
* [Upgrading to High DefinitionRender Pipeline (URP)][upgrade-hdrp]




# Getting Started

This plugin has been configured to be used with the [Unity Package Manager (UPM)][package-manager], but we also provide a \*.unitypackage for those who wish to follow the old approach which can be obtained from [our developer site][devsite] or from the [release section][releases].

Be sure to also check out our [documentation][documentation] and [API reference][api-reference].

## Installation

1. __UPM via GitHub__  
  To add a (read-only) UPM package remotely via a GitHub URL select the option "Add package from git URL…" in the Unity package manager and paste in the textbox that appears. The package will then be added to your project and you should be good to go! 
    * *Requires Unity 2019.4 (LTS) or newer.*

2. __UPM Local Package__  
  To add a (read-only) UPM package locally from a cloned repository select the option "Add package from disk…" and point it at the root folder of the cloned repository on your file system.
    * *By default this will use an absolute file path from your machine, so will not be a sharable solution without some modification.*

3. __UPM Embedded Package__  
  To add a (editable) UPM package locally from a cloned repository place it within the Packages folder of your Unity project.
    * *This is perhaps the easiest way to work if you want o submit a pull request against the Ultraleap Unity Plugin.*

4. __Unity Package__  
  Import the package (e.g. Assets -> Import Package -> Custom Package...) which can be downloaded from [our Unity developer site][devsite] or the [releases section][releases] of this repository.     
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
