
<!--links-->
[package-manager]: https://docs.unity3d.com/Manual/Packages.html
[documentation]: https://docs.ultraleap.com/ "Ultraleap UnityPlugin Documentation"
[api-reference]: https://docs.ultraleap.com/ "Ultraleap UnityPlugin API Reference"
[devsite]: https://developer.leapmotion.com/unity/ "Ultraleap Developers site"
[apache]: http://www.apache.org/licenses/LICENSE-2.0 "Apache V2 License"
[releases]: https://github.com/leapmotion/UnityModules/releases

<!--content-->
# Ultraleap Unity Plugin

To download Ultraleap's latest stable release as \*.unitypackages, visit [our Unity developer site][devsite] or the [releases section][releases] of this repository. Be sure to also check out our [documentation][documentation] and [API reference][api-reference]!

## Introduction

This repository hosts the complete Ultraleap Unity Plugin and developer SDK. Examples are included to quickly get you up and running with Ultraleap's hand tracking technology.


## Requirements

You will need a Leap Motion compatible device to use this plugin.

You will need to have the Leap Motion service installed, which must be downloaded separately:
  https://developer.leapmotion.com/

Use of Ultraleap's UnityModules is subject to the [Apache V2 License Agreement][apache].

# Getting Started

This repository contains the code for the Ultraleap Unity Plugin.

## Installation

There are several ways you can consume this plugin.

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

Please visit [this link][package-manager] for more information in general about the Unity Package Manager.

## Upgrading Existing Installation

//TODO