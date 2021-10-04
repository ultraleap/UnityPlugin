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



<!--content-->
# Ultraleap Unity Plugin

## Introduction

Ultraleap's Unity Plugin enables the data produced by Ultraleap tracking hardware to be used by developers inside their Unity projects. It includes various utilities, examples and prefabs that make it as easy as possible to design and use hand tracking in XR projects

This repository hosts the complete Ultraleap Unity Plugin and Ultraleap Tracking SDK. Examples are included to quickly get you up and running with Ultraleap's hand tracking technology.

## Getting Started

This repository contains code for Ultraleap's Unity Plugin 5.0 which has been designed to be an easy-to-use tool for integrating Ultraleap cameras into new Unity projects. However, there are a couple of things you will need to be able to test the content you have created, and there are also several ways you can go about installing Ultraleap’s Unity Plugin 

### Prerequisites

To use this Plugin you will need the following:

1. The latest Ultraleap Tracking Service installed
2. An Ultraleap compatible device 

### Installation

The Unity Plugin repository is designed to work with Unity 2019.4 LTS or newer, as a result there are several ways you can consume this plugin 


#### Option 1: UPM via GitHub
<details><summary>Click to expand</summary>

  - To add a (read-only) UPM package remotely via a GitHub URL select the option "Add package from git URL…" in the Unity package manager. 
  - Paste the link to [clone the repository][repository-clone-url] into the input field that appears and press enter. 
  - The package will then be added to your project and you should be good to go!  
  - *Requires Unity 2019.4 (LTS) or newer.*

</details>

#### Option 2: UPM Local Package
<details><summary>Click to expand</summary>

  - To add a (read-only) UPM package locally from a cloned repository select the option "Add package from disk…" and point it at the root folder of the cloned repository on your file system.  
  - *By default this will use an absolute file path from your machine, so will not be a sharable solution without some modification.*

</details> 

#### Option 3: UPM Embedded Package
<details><summary>Click to expand</summary>

  -  To add a (editable) UPM package locally from a cloned repository place it within the Packages folder of your Unity project.  
  - *This is perhaps the easiest way to work if you want o submit a pull request against the Ultraleap Unity Plugin.*

</details>
  
#### Option 4: Unity Package
<details><summary>Click to expand</summary>

  - Import the package (e.g. Assets -> Import Package -> Custom Package...) which can be downloaded from [our Unity developer site][developer-site-unity] or the [releases section][releases] of this repository.     
  - *\*.unitypackage(s) are a deprecated solution in Unity. Do not move the location of the installed plugin as this may break certain features.*

</details>
  
#### Option 5: Submodule
<details><summary>Click to expand</summary>

  - You can also add this plugin as a submodule in your assets folder. 
  - *Use this method with caution as submodules can introduce their own complexities to a project*

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

## Usage

#### Core 

  Contains the minimum functionality required for the visualisation of 3D hands. Should help you to understand where to begin with hand tracking

  - We include objects attached to the hand tracking data to help users understand this relationship
  - We show hand tracking working in a range of tracking orientations
  - We show you that incoming hand tracking data can be manipulated
  - We include an infrared camera feed with a 3D hand model tracking to a real hand
  - We show how a rigged hand could be built before a user starts this aspect of the workflow so that they can understand how the rig will work once hand tracking data has been applied

 
#### Interaction Engine 

  A collection of functions that allow you to determine physics-driven interactions between hands and other objects. Used to determine the correct velocity of a hand to determine how much / far a 3D object should move

  - We include a scene that shows hand tracking working with complex shapes, allowing the user to pick up and interact with objects in the scene
  - We have an example to show how to interact with UI Elements
  - We include an example showing UI attached to the hand (as opposed to fixed in the scene)

#### Hands 

  Enables developers to use hand tracking data to drive their own 3D Hand assets without writing any code, includes sample hand assets. Can be used to include any custom hand visuals or bind hand tracking data to things in your scene.

  - We provide different styles of 3D hands that you can use
  - We have in-depth documentation online with an explanation of each feature
  - We have included step by step guides within the Editor which teaches you how to set up hands without the need to open online documentation
  - No programming knowledge is needed  

#### UI Input: 

Enables developers to retrofit their existing 2D UIs so that they can be interacted with using hand tracking. Helps developers to get started with hand tracking without needing to build something from scratch

****Discover more about our recommended examples and the applicable use cases in our [XR Design Guidelines][xr-guidelines]****.

## Contributing

Our vision is to make it as easy as possible to design the best user experience for hand tracking use cases in VR. Contributions are what make the open source community such an amazing place to learn, inspire, and create. Any contributions you make are greatly appreciated.

1. Fork the Project
2. Create your Feature Branch (git checkout -b feature/AmazingFeature)
3. Commit your Changes (git commit -m 'Add some AmazingFeature')
4. Push to the Branch (git push origin feature/AmazingFeature)
5. Open a Pull Request

## License
Use of Ultraleap's Unity Plugin is subject to the [Apache V2 License Agreement][apache].

## Contact
User Support: support@ultraleap.com 

## Community Support
Our [Developer Forum][developer-forum] is a place where you are actively encouraged to share your questions, insights, ideas, feature requests and projects. 

## Links 
[Ultraleap Unity Plugin][repository-clone-url]
