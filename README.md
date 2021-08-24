# Ultraleap Unity Plugin

To download Ultraleap's latest stable modules as .unitypackages, visit [our Unity developer site][devsite]. Be sure to also check out our [documentation and API reference][um-docs]!

## Introduction

This repository hosts the complete Ultraleap Unity Plugin and developer SDK.


## Requirements

You will need a Leap Motion compatible device to use this plugin.

You will need to have the Leap Motion service installed, which must be downloaded separately:
  https://developer.leapmotion.com/

Use of Ultraleap's UnityModules is subject to the [Apache V2 License Agreement][apache].

## Getting Started

This repository contains code for Ultraleap's Unity Modules, easy-to-use tools for integrating the Ultraleap camera hardware in Unity projects, and various utilities for VR and AR projects.

## Installation

There are several ways you can consume this plugin.

1. OpenUPM
Add the following scoped registry to Unity (Edit -> Project Settings... -> Package Manager -> Scoped Registries) 
  Name: Ultraleap - OpenUPM
  URL: https://package.openupm.com
  Scope(s): com.ultraleap
  Then select "My Registries" from the package manager and install the Ultraleap Unity plugin.
2. UPM via GitHub
To add a UPM package remotely via a GitHub URL select the option "Add package from git URL…" in the Unity package manager and paste in the textbox that appears. The package will then be added to your project and you should be good to go! __N.B. Requires Unity 2019.4 (LTS) or newer.__
3. UPM Local Package
To add a package locally from a cloned repository select the option "Add package from disk…" and point it at the root folder of the cloned repository on your file system. __N.B. By default this will use an absolute file path from your machine, so will not be a sharable solution without some modification.__
4. Unity Package
Import the package (e.g. Assets -> Import Package -> Custom Package...) which can be downloaded from this link (<TODO>Add link</TODO>). __N.B. *.unitypackage(s) are a deprecated solution in Unity. Do not move the location of the installed plugin as this may break certain features.__
5. Submodule
You can also add this plugin as a submodule in your assets folder 


[um-docs]: https://leapmotion.github.io/UnityModules/
[devsite]: https://developer.leapmotion.com/unity/ "Ultraleap for Developers site"
[wiki]: https://github.com/leapmotion/UnityModules/wiki "Ultraleap / Leap Motion Unity Modules Wiki"
[apache]: http://www.apache.org/licenses/LICENSE-2.0 "Apache V2 License"
[releases]: https://github.com/leapmotion/UnityModules/releases
