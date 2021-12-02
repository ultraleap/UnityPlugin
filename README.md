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
[repository-url]: https://github.com/ultraleap/UnityPlugin.git "Repository URL"
[repository-tags]: https://github.com/ultraleap/UnityPlugin/tags "UnityPlugin tags"
[upm-giturl-installing]: https://docs.unity3d.com/Manual/upm-ui-giturl.html "Installing a UPM package from a Git URL"
[upm-giturl-dependencies]: https://docs.unity3d.com/Manual/upm-git.html "UPM Git dependencies"
[upm-giturl-revision]: https://docs.unity3d.com/Manual/upm-git.html#revision "Targeting a specific revision"
[upm-giturl-locks]: https://docs.unity3d.com/Manual/upm-git.html#git-locks "Locked Git dependencies"
[upm-localpath]: https://docs.unity3d.com/Manual/upm-localpath.html "UPM local packages"
[upm-troubleshooting]: https://docs.unity3d.com/Manual/upm-errors.html "UPM Troubleshooting Page"
[xr-guidelines]: https://docs.ultraleap.com/xr-guidelines/ "XR Guidelines"

[xr-legacy-input-helpers-documentation]: http://docs.unity3d.com/Packages/com.unity.xr.legacyinputhelpers@2.1/manual/index.html "XR Legacy Input Helpers"
[oculus-xr-documentation]: http://docs.unity3d.com/Packages/com.unity.xr.oculus@1.4/manual/index.html "Oculus XR package"



<!--content-->
# Ultraleap Unity Plugin

## Introduction

The Ultraleap Hand Tracking Plugin for Unity enables Ultraleap's hand tracking data to be used by developers inside their Unity projects. It includes various utilities, examples, and prefabs that make it as easy as possible to design and use hand tracking in XR projects. Examples are included to quickly get you up and running with Ultraleap's hand tracking technology.

## Getting Started

This repository contains code for Ultraleap's Unity Plugin – an easy-to-use tool for integrating Ultraleap Hand Tracking Cameras into new Unity projects. There are a couple of things you will need to be able to test the content you have created, and there are also several ways you can go about installing Ultraleap’s Unity Plugin. 

### Requirements

To use this Plugin you will need the following:

1. The latest Ultraleap Hand Tracking Software
2. An Ultraleap Hand Tracking Camera
3. Unity 2019.4 LTS or newer
4. Windows® 10, 64-bit

### Installation

The UnityPlugin repository is designed and tested to work against 2019.4 LTS and 2020.3 LTS. 

There are several ways you can consume this plugin. We've listed several recommended workflows for different users below. This list is by no means exhaustive - if you are aware of a workflow not listed that you think others would appreciate, please consider contributing!

> **_NOTE:_** All UPM workflows require Unity 2019.4 LTS or newer (some older versions may work with UPM but are no longer supported by Unity).

#### Consumer Workflows

Consumer workflows are recommended for UnityPlugin users that don't require the ability to contribute back to the source repository.

<details>
<summary> UPM Package via OpenUPM </summary>

##### OpenUPM Summary

This workflow is the easiest way to get up and running and makes updating packages simple.

##### OpenUPM Setup

Setup only needs to be performed once per Unity project.
In `Edit -> Project Settings -> Package Manager`, add a new scoped registry with the following details:

    Name: Ultraleap
    URL: https://package.openupm.com
    Scope(s): com.ultraleap
  
  ![scoped_registry.png](Markdown/images/scopedregistry.png)

##### OpenUPM Adding, Upgrading or Removing Packages
    
  Open the Package Manager (`Window -> Package Manager`) and navigate to "My Registries" in the dropdown at the top left of the window.

  ![my_registries.png](Markdown/images/myregistries.png)

  Ultraleap UPM packages should be available in the list. Click on the package you wish to modify.

  ![packagelist.png](Markdown/images/packagelist.png)

  (Optional) When clicking the package, it will automatically select the latest version. If you want to pick or change to a different version, click the arrow on the left of the package name and then "See all versions".

  ![packageversions.png](Markdown/images/packageversions.png)

  The package can be installed or removed using buttons in the bottom right. (The install button is replaced with "Upgrade to \<version\>" if the package is currently installed)

  ![packageinstall.png](Markdown/images/packageinstall.png)

##### OpenUPM CLI

If you prefer to use a CLI to modify your packages or need to be able to perform actions from a terminal (e.g. CI) then you may find the OpenUPM CLI helpful.
See [Getting Started with OpenUPM-CLI](https://openupm.com/docs/getting-started.html#installing-openupm-cli).

</details>

<details>
<summary> UPM Package via Git URL </summary>

##### Git URL Summary

Git URL is available as another option to consume the UnityPlugin as a UPM package.

> Git URL is not recommended for several reasons:
> - Version is non-deterministic (will resolve to what the latest is at the time of import) unless explicitly handled as part of the URL.
> - Requires specifying the path within the repository to the package being installed.
> - Does not resolve dependencies automatically.
> - Discovering versions requires looking through repository tags.
>
> If the OpenUPM workflow does not meet your needs, consider using the Local UPM Package contributor workflow instead of a Git URL. It is not suspectable to the non-deterministic version pitfall and will resolve dependencies automatically. However, the contributor workflow requires using git to change between versions.
> Another alternative is to use 

The headings below will guide you in accomplishing specific tasks tailored to the UnityPlugin but for more details it is recommended to read Unity's documentation for [installing using Git URL][upm-giturl-installing] and handling [Git dependencies][upm-giturl-dependencies].

Ultraleap Package URLs (without revision)
- Tracking Package - `https://github.com/ultraleap/UnityPlugin.git?path=/Packages/Tracking`
- Tracking Preview Package - `https://github.com/ultraleap/UnityPlugin.git?path=/Packages/Tracking%20Preview`

##### Git URL Adding Packages

1. To add a UPM package remotely via a GitHub URL select the option "Add package from git URL…" in the Unity package manager. ![addgiturl.png](Markdown/images/addgiturl.png)
1. (Optional) Unless you're ok with a non-deterministic version, determine which package version you want to target from the [tags][repository-tags]. You can also target a commit or branch - see [targeting a specific revision][upm-giturl-revision].
1. Copy and paste one of the Package URL links above (modifying it to target the revision, if you picked one - e.g. to target version 5.0.0 `https://github.com/ultraleap/UnityPlugin.git?path=/Packages/Tracking#v5.0.0`) into the input field that appears and press enter. 
1. The package will then be added to your project and you should be good to go!

##### Git URL Upgrading Packages

1. Follow the same steps as adding a package to upgrade (with a different target revision if you are handling versioning explicitly). See [locked git dependencies][upm-giturl-locks] for more info.

##### Git URL Removing Packages

1. Open the package manager (`Window -> Package Manager`).
1. Navigate to "In Project" in the dropdown. ![packagesinproject.png](Markdown/images/packagesinproject.png)
1. Select the package you want to remove and click remove in the bottom right.

</details>

<details>
<summary> Legacy .unitypackage </summary>

##### .unitypackage Summary

.unitypackage files are the legacy consumption method available if you prefer it or which still can be helpful if you:
1. need to modify the package content _and_
2. don't expect to upgrade to a newer version

> If you don't need to modify package content, the OpenUPM Consumer workflow is recommended.
> If you do and expect to upgrade to a newer version, the Local UPM Package Contributor workflow is recommended as it enables you to version control your changes using git and resolve any potential conflicts when upgrading.

##### .unitypackage Adding

1. Import the package (`Assets -> Import Package -> Custom Package`) which can be downloaded from [our Unity developer site][developer-site-unity] or the [releases section][releases] of this repository.

##### .unitypackage Upgrading

1. (Optional) If you have made any changes to a package you may want to save those changes elsewhere.
1. Delete the package content you want to upgrade from `Assets/ThirdParty/Ultraleap`.
1. Import the .unitypackage you wish to change to.

##### .unitypackage Removing

1. Delete the package you want to remove from `Assets/ThirdParty/Ultraleap`.

</details>

#### Contributor Workflows

Contributor workflows are used by UnityPlugin developers and are recommended for community members that want to contribute back to the source repository.

<details>
<summary> Local UPM Package </summary>

##### Local UPM Package Summary

This workflow takes a few steps to setup and enables you to:
- Modify UPM package content from within one (or many) Unity project(s).
- Manage changes using git.
- Contribute changes back to the remote repository.

##### Local UPM Package Setup

1. Clone or submodule the [repository][repository-url].
    1. The repository should not be cloned/submoduled into Unity reserved project folders, i.e. Assets, Library, ProjectSettings or Packages. Creating another folder such as "LocalPackages" is recommended.
    1. (Note) If you don't plan to share your project and would like to use the same UPM packages across multiple Unity projects it may be ideal to clone to a common place on your machine.

##### Local UPM Package Adding

You can add packages from the repository to your project in one of two ways:
1. (Sharable) Edit your project manifest.json (`Project/Packages/manifest.json`) to add the relative paths from your Unity project's Packages folder to the Packages in the repository Packages folder.
      For more information see the [Unity Manual](https://docs.unity3d.com/Manual/upm-localpath.html).
      Below is an example if you had cloned the repository to LocalPackages within your Unity project.
      ```
      "com.ultraleap.tracking": "file:../LocalPackages/unityplugin/Packages/Tracking",
      "com.ultraleap.tracking.preview": "file:../LocalPackages/unityplugin/Packages/Tracking Preview",
      ```
    
1. (Not sharable) Open the package manager (`Window -> Package Manager`) and click "Add package from disk…". Point it to the desired package within the repository `Packages` folder.
Repeat to add all the packages you want to reference locally.
*This will use an absolute file path from your machine, so will not be a sharable solution without modifying the path to work on the new machine.*

##### Local UPM Package Upgrading

Changing package versions is done through the git repository itself. Released versions can be found by checking the repository tags.

##### Local UPM Package Removing

1. Open the package manager (`Window -> Package Manager`).
1. Navigate to "In Project" in the dropdown. ![packagesinproject.png](Markdown/images/packagesinproject.png)
1. Select the package you want to remove and click remove in the bottom right.

</details>

**Please note:**  
- Due to the ever changing landscape of package dependencies in Unity we cannot guarantee compatibility with every plugin or variant of Unity, but aim to provide support for any LTS versions that are under continuous support from Unity. 
- If you are sourcing the Unity Plugin directly from this repository, you may find that it does not function well with earlier versions of Unity.

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

### UPM Troubleshooting

Unity's [upm-troubleshooting] can help with common issues encountered when using UPM packages. If you have any questions or problems see [Contact](#contact) or [Community Support](#community-support).

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
    git checkout -b feature/AmazingFeature
3. Commit your Changes:  
    git commit -m "Add some AmazingFeature"
4. Push to the Branch:   
    git push origin feature/AmazingFeature
5. Open a Pull Request

### License
Use of Ultraleap's Unity Plugin is subject to the [Apache V2 License Agreement][apache].

## Contact
User Support: support@ultraleap.com 

## Community Support
Our [Developer Forum][developer-forum] is a place where you are actively encouraged to share your questions, insights, ideas, feature requests and projects. 

## Links 
[Ultraleap Unity Plugin][repository-url]
