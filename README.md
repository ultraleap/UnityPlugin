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
[xr-guidelines]: https://docs.ultraleap.com/xr-guidelines/ "XR Guidelines"

[xr-legacy-input-helpers-documentation]: http://docs.unity3d.com/Packages/com.unity.xr.legacyinputhelpers@2.1/manual/index.html "XR Legacy Input Helpers"
[oculus-xr-documentation]: http://docs.unity3d.com/Packages/com.unity.xr.oculus@1.4/manual/index.html "Oculus XR package"


<!--content-->
# Ultraleap Unity Plugin

## Introduction

Ultraleap's Unity Plugin enables the data produced by integrating Ultraleap's hand tracking data to be used by developers inside their Unity projects. It includes various utilities, examples and prefabs that make it as easy as possible to design and use hand tracking in XR projects. Examples are included to quickly get you up and running with Ultraleap's hand tracking technology.

## Getting Started

This repository contains code for Ultraleap's Unity Plugin which has been designed to be an easy-to-use tool for integrating Ultraleap cameras into new Unity projects. However, there are a couple of things you will need to be able to test the content you have created, and there are also several ways you can go about installing Ultraleap’s Unity Plugin. 

### Prerequisites

*N.B. This plugin only supports 64-bit Windows builds*

To use this Plugin you will need the following:

1. The latest Ultraleap Tracking Service installed
2. An Ultraleap compatible device 
3. Unity 2019.4 LTS or newer

### Installation

The Unity Plugin repository is designed and tested to work against 2019.4 LTS and 2020.3 LTS. 

There are several ways you can consume this plugin.

<details>
<summary> Option 1: UPM via GitHub </summary>

  - To add a (read-only) UPM package remotely via a GitHub URL select the option "Add package from git URL…" in the Unity package manager. 
  - Paste the link to [clone the repository][repository-clone-url] into the input field that appears and press enter. 
  - The package will then be added to your project and you should be good to go!  
  - *Requires Unity 2019.4 (LTS) or newer.*

</details>

<details>
<summary> Option 2: UPM Local Package </summary>

  - To add a (read-only) UPM package locally from a cloned repository select the option "Add package from disk…" and point it at the root folder of the cloned repository on your file system.
  - *By default this will use an absolute file path from your machine, so will not be a sharable solution without some modification.*
  - *Requires Unity 2019.4 (LTS) or newer.*

</details> 

<details>
<summary> Option 3: UPM Embedded Package </summary>

  -  To add an (editable) UPM package locally from a cloned repository place it within the Packages folder of your Unity project.  
  - *This is perhaps the easiest way to work if you want o submit a pull request against the Ultraleap Unity Plugin.*
  - *Requires Unity 2019.4 (LTS) or newer.*

</details>
  
<details>
<summary> Option 4: Unity Package (*.unitypackage) </summary>

  - Import the package (e.g. Assets -> Import Package -> Custom Package...) which can be downloaded from [our Unity developer site][developer-site-unity] or the [releases section][releases] of this repository.     
  - *\*.unitypackage(s) are a deprecated solution in Unity. Do not move the location of the installed plugin as this may break certain features.*

</details>

<details>
<summary> Option 5: Submodule </summary>

  - You can also add this plugin as a submodule in your assets folder. 
  - *We do not recommend this approach. Use this method with caution as submodules can introduce their own complexities to a project*

</details>

**Please note:**  
- Due to the ever changing landscape of package dependencies in Unity we cannot guarantee compatibility with every plugin or variant of Unity, but aim to provide support for any LTS versions that are under continuous support from Unity. 
- If you are sourcing the Unity Plugin directly from this repository, you may find that it does not function well with earlier versions of Unity

### Dependencies

If you are using Unity 2020.4 (LTS) or newer with XR then you will need to follow the Unity documentation on how to configure your project.
  - [Upgrading to XR plugin Management System][upgrade-xr]
  
If you are using any of the scriptable render pipelines (SRP) then you will need to follow the appropriate Unity documentation for upgrading shaders:
  - [Upgrading to Universal Render Pipeline (URP)][upgrade-urp] 
  - [Upgrading to High Definition Render Pipeline (HDRP)][upgrade-hdrp]

If you are using Unity 2019.4 (LTS) and you get errors related to "SpatialTracking" upon importing, you will need to install the following package:
  - [XR Legacy Input Helpers][xr-legacy-input-helpers-documentation].

If you are using Unity 2020.1 or newer and you get errors related to "SpatialTracking" upon importing, you will need to install the following package:
  - [Oculus XR package][oculus-xr-documentation].


## Usage

#### Core 

  Contains the minimum functionality required for the visualisation of 3D hands - it is everything you need to get started with hand tracking.

  - We include objects attached to the hand tracking data to help users understand this relationship
  - We show hand tracking working in a range of tracking orientations
  - We show you that incoming hand tracking data can be manipulated
  - We include an infrared camera feed with a 3D hand model tracking to a real hand

 
#### Interaction Engine 

  The Interaction Engine provides physics representations of hands and VR controllers fine-tuned with interaction heuristics to provide a fully-featured interaction API: grasping, throwing, stable 'soft' collision feedback, and proximity. It also comes with with a suite of examples and prefabs to power reliable, stable 3D user interfaces as well as any physics-critical experiences.

  - We include a scene that shows hand tracking working with complex shapes, allowing the user to pick up and interact with objects in the scene
  - We have an example to show how to interact with Unity UI
  - We include an example showing UI attached to the hand (as opposed to fixed in the scene)

#### Hands 

  Enables developers to use hand tracking data to drive their own 3D Hand assets without writing any code, includes sample hand assets. Can be used to include any custom hand visuals or bind hand tracking data to things in your scene.

  - We provide different styles of 3D hands that you can use
  - We have in-depth documentation online with an explanation of each feature
  - We have included step by step guides within the Editor which teaches you how to set up hands without the need to open online documentation
  - No programming knowledge is needed  
  - We provide shaders to support HDRP/URP & the Standard render pipeline.

#### UI Input: 

Enables developers to retrofit their existing 2D UIs so that they can be interacted with using hand tracking. Helps developers to get started with hand tracking without needing to build something from scratch

****Discover more about our recommended examples and the applicable use cases in our [XR Design Guidelines][xr-guidelines]****.

### Contributing

Our vision is to make it as easy as possible to design the best user experience for hand tracking use cases in VR. We learn and are inspired by the creations from our open source community - any contributions you make are greatly appreciated.

1. Fork the Project
2. Create your Feature Branch:  
```git checkout -b feature/AmazingFeature```
3. Commit your Changes:  
```git commit -m "Add some AmazingFeature"```
4. Push to the Branch:   
```git push origin feature/AmazingFeature```
5. Open a Pull Request

### License
Use of Ultraleap's Unity Plugin is subject to the [Apache V2 License Agreement][apache].

## Contact
User Support: support@ultraleap.com 

## Community Support
Our [Developer Forum][developer-forum] is a place where you are actively encouraged to share your questions, insights, ideas, feature requests and projects. 

## Links 
[Ultraleap Unity Plugin][repository-clone-url]
