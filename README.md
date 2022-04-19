<!--links-->
[apache]: http://www.apache.org/licenses/LICENSE-2.0 "Apache V2 License"

[documentation]: https://docs.ultraleap.com/ "Ultraleap UnityPlugin Documentation"
[xr-guidelines]: https://docs.ultraleap.com/xr-guidelines/ "XR Guidelines"

[developer-site]: https://developer.leapmotion.com/ "Ultraleap Developer Site"
[developer-site-tracking-software]: https://developer.leapmotion.com/tracking-software-download "Ultraleap Tracking Software"
[developer-site-setup-camera]: https://developer.leapmotion.com/setup-camera "Ultraleap Setup Camera"
[developer-site-unity]: https://developer.leapmotion.com/unity/ "Ultraleap Developer site - Unity"
[developer-forum]: https://forums.leapmotion.com/ "Developer Forum"

[releases]: https://github.com/ultraleap/UnityPlugin/releases "UnityPlugin releases"
[repository-url]: https://github.com/ultraleap/UnityPlugin.git "Repository URL"
[repository-tags]: https://github.com/ultraleap/UnityPlugin/tags "UnityPlugin tags"

[upgrade-urp]: https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@7.1/manual/upgrading-your-shaders.html "Unity URP Upgrade Documentation"
[upgrade-hdrp]: https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@7.1/manual/Upgrading-To-HDRP.html "Unity HDRP Upgrade Documentation"
[upgrade-xr]: https://docs.unity3d.com/Manual/configuring-project-for-xr.html "Unity XR Upgrade Documentation"
[package-manager]: https://docs.unity3d.com/Manual/Packages.html "Unity Package Manager Documentation"
[upm-giturl-installing]: https://docs.unity3d.com/Manual/upm-ui-giturl.html "Installing a UPM package from a Git URL"
[upm-giturl-dependencies]: https://docs.unity3d.com/Manual/upm-git.html "UPM Git dependencies"
[upm-giturl-revision]: https://docs.unity3d.com/Manual/upm-git.html#revision "Targeting a specific revision"
[upm-giturl-locks]: https://docs.unity3d.com/Manual/upm-git.html#git-locks "Locked Git dependencies"
[upm-localpath]: https://docs.unity3d.com/Manual/upm-localpath.html "UPM local packages"
[upm-troubleshooting]: https://docs.unity3d.com/Manual/upm-errors.html "UPM Troubleshooting Page"
[xr-legacy-input-helpers-documentation]: http://docs.unity3d.com/Packages/com.unity.xr.legacyinputhelpers@2.1/manual/index.html "XR Legacy Input Helpers"
[oculus-xr-documentation]: http://docs.unity3d.com/Packages/com.unity.xr.oculus@1.4/manual/index.html "Oculus XR package"

[openupm-cli]: https://openupm.com/docs/getting-started.html#installing-openupm-cli "OpenUPM CLI"

<!--content-->
# Ultraleap Unity Plugin

[![documentation](https://img.shields.io/badge/Documentation-docs.ultraleap.com-00cf75)][documentation]
[![forum](https://img.shields.io/badge/Community-Developer%20Forum-00cf75)][developer-forum]
[![mail](https://img.shields.io/badge/Contact-support%40ultraleap.com-00cf75)](mailto:support@ultraleap.com)
![GitHub](https://img.shields.io/github/license/ultraleap/UnityPlugin)

[![openupm-tracking](https://img.shields.io/npm/v/com.ultraleap.tracking?label=OpenUPM%20Tracking&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.ultraleap.tracking/)
[![openupm-tracking-preview](https://img.shields.io/npm/v/com.ultraleap.tracking.preview?label=OpenUPM%20Tracking%20Preview&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.ultraleap.tracking.preview/)
[![openupm-tracking-openxr](https://img.shields.io/npm/v/com.ultraleap.tracking.openxr?label=OpenUPM%20Tracking%20OpenXR&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.ultraleap.tracking.openxr/)

The Ultraleap Unity Plugin empowers developers to build Unity applications using Ultraleap's hand tracking technology. It includes various assets, examples, and utilities that make it easy to design and build applications using hand tracking in XR projects.

## Getting Started

To use this plugin you will need the following:

1. The latest Ultraleap Hand Tracking Software. You can get this [here][developer-site-tracking-software].
2. An Ultraleap Hand Tracking Camera - follow setup process [here][developer-site-setup-camera].
3. Unity 2019.4 LTS or newer. UnityPlugin packages have been tested to work against 2019.4 LTS and 2020.3 LTS.
4. Windows® 10, 64-bit
5. Follow one of the Installation workflows listed below.

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

## Installation

There are several ways you can consume this plugin. We've listed several recommended workflows for different users below. This list is by no means exhaustive - if you are aware of a workflow not listed that you think others would appreciate, please consider contributing!

### Consumer Workflows

Consumer workflows are recommended for UnityPlugin users that don't require the ability to contribute back to the source repository.

<details>
<summary> UPM Package via OpenUPM </summary>

#### OpenUPM Summary

This workflow is the easiest way to get up and running and makes updating packages simple.

#### OpenUPM Setup

Setup only needs to be performed once per Unity project.
In `Edit -> Project Settings -> Package Manager`, add a new scoped registry with the following details:

    Name: Ultraleap
    URL: https://package.openupm.com
    Scope(s): com.ultraleap
  
  ![scoped_registry.png](Markdown/images/scopedregistry.png)

#### OpenUPM Adding, Upgrading or Removing Packages

  Open the Package Manager (`Window -> Package Manager`) and navigate to "My Registries" in the dropdown at the top left of the window.

  ![my_registries.png](Markdown/images/myregistries.png)

  Ultraleap UPM packages should be available in the list. Click on the package you wish to install/modify.
  
  Note: Ultraleap Tracking contains the Core, Hands and Interaction Engine modules. There are older packages created indepentently by a third party for these modules that are no longer updated.

  ![packagelist.png](Markdown/images/packagelist.png)

  (Optional) When clicking the package, it will automatically select the latest version. If you want to pick or change to a different version, click the arrow on the left of the package name and then "See all versions".

  ![packageversions.png](Markdown/images/packageversions.png)

  The package can be installed or removed using buttons in the bottom right. (The install button is replaced with "Upgrade to \<version\>" if the package is currently installed)

  ![packageinstall.png](Markdown/images/packageinstall.png)

#### OpenUPM CLI

If you prefer to use a CLI to modify your packages or need to be able to perform actions from a terminal (e.g. CI) then you may find the OpenUPM CLI helpful.
See [Getting Started with OpenUPM-CLI][openupm-cli].

</details>

<details>
<summary> UPM Package via Git URL </summary>

#### Git URL Summary

Git URL is available as another option to consume the UnityPlugin as a UPM package.

> Git URL is not recommended for several reasons:
>
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

#### Git URL Adding Packages

1. To add a UPM package remotely via a GitHub URL select the option "Add package from git URL…" in the Unity package manager. ![addgiturl.png](Markdown/images/addgiturl.png)
1. (Optional) Unless you're ok with a non-deterministic version, determine which package version you want to target from the [tags][repository-tags]. You can also target a commit or branch - see [targeting a specific revision][upm-giturl-revision].
1. Copy and paste one of the Package URL links above (modifying it to target the revision, if you picked one - e.g. to target version 5.0.0 `https://github.com/ultraleap/UnityPlugin.git?path=/Packages/Tracking#v5.0.0`) into the input field that appears and press enter. 
1. The package will then be added to your project and you should be good to go!

#### Git URL Upgrading Packages

1. Follow the same steps as adding a package to upgrade (with a different target revision if you are handling versioning explicitly). See [locked git dependencies][upm-giturl-locks] for more info.

#### Git URL Removing Packages

1. Open the package manager (`Window -> Package Manager`).
1. Navigate to "In Project" in the dropdown. ![packagesinproject.png](Markdown/images/packagesinproject.png)
1. Select the package you want to remove and click remove in the bottom right.

</details>
<details>

<summary> Legacy .unitypackage </summary>

#### .unitypackage Summary

.unitypackage files are the legacy consumption method available if you prefer it or which still can be helpful if you:

1. need to modify the package content _and_
2. don't expect to upgrade to a newer version

> If you don't need to modify package content, the OpenUPM Consumer workflow is recommended.
> If you do and expect to upgrade to a newer version, the Local UPM Package Contributor workflow is recommended as it enables you to version control your changes using git and resolve any potential conflicts when upgrading.

#### .unitypackage Adding

1. Import the package (`Assets -> Import Package -> Custom Package`) which can be downloaded from [our Unity developer site][developer-site-unity] or the [releases section][releases] of this repository.

#### .unitypackage Upgrading

1. (Optional) If you have made any changes to a package you may want to save those changes elsewhere.
1. Delete the package content you want to upgrade from `Assets/ThirdParty/Ultraleap`.
1. Import the .unitypackage you wish to change to.

#### .unitypackage Removing

1. Delete the package you want to remove from `Assets/ThirdParty/Ultraleap`.

</details>

### Contributor Workflows

Contributor workflows are used by UnityPlugin developers and are recommended for community members that want to contribute back to the source repository.

<details>
<summary> Local UPM Package </summary>

#### Local UPM Package Summary

This workflow takes a few steps to setup and enables you to:

- Modify UPM package content from within one (or many) Unity project(s).
- Manage changes using git.
- Contribute changes back to the remote repository.

#### Local UPM Package Setup

1. Clone or submodule the [repository][repository-url].
    1. The repository should not be cloned/submoduled into Unity reserved project folders, i.e. Assets, Library, ProjectSettings or Packages. Creating another folder such as "LocalPackages" is recommended.
    1. (Note) If you don't plan to share your project and would like to use the same UPM packages across multiple Unity projects it may be ideal to clone to a common place on your machine.

#### Local UPM Package Adding

You can add packages from the repository to your project in one of two ways:

1. (Sharable) Edit your project manifest.json (`Project/Packages/manifest.json`) to add the relative paths from your Unity project's Packages folder to the Packages in the repository Packages folder.
      For more information see the [Unity Manual][upm-localpath].
      Below is an example if you had cloned the repository to LocalPackages within your Unity project.

       "com.ultraleap.tracking": "file:../LocalPackages/unityplugin/Packages/Tracking",
       "com.ultraleap.tracking.preview": "file:../LocalPackages/unityplugin/Packages/Tracking Preview",

1. (Not sharable) Open the package manager (`Window -> Package Manager`) and click "Add package from disk…". Point it to the desired package within the repository `Packages` folder.
Repeat to add all the packages you want to reference locally.
*This will use an absolute file path from your machine, so will not be a sharable solution without modifying the path to work on the new machine.*

#### Local UPM Package Upgrading

Changing package versions is done through the git repository itself. Released versions can be found by checking the repository tags.

#### Local UPM Package Removing

1. Open the package manager (`Window -> Package Manager`).
1. Navigate to "In Project" in the dropdown. ![packagesinproject.png](Markdown/images/packagesinproject.png)
1. Select the package you want to remove and click remove in the bottom right.

</details>

### UPM Troubleshooting

Unity's [upm-troubleshooting] can help with common issues encountered when using UPM packages. If you have any questions or problems see [Contact](#contact) or [Community Support](#community-support).

## Key Features

The Tracking package offers Ultraleap's core functionality, as well as a variety of additional components which we think are fundamental to an unforgettable hands-first experience.

### Core

Contains the minimum functionality required for the visualisation of 3D hands - it is everything you need to get started with hand tracking.

- We include objects attached to the hand tracking data to help users understand this relationship
- We show hand tracking working in a range of tracking orientations
- We show you that incoming hand tracking data can be manipulated
- We include an infrared camera feed with a 3D hand model tracking to a real hand

### Interaction Engine

The Interaction Engine provides physics representations of hands and VR controllers fine-tuned with interaction heuristics to provide a fully-featured interaction API: grasping, throwing, stable 'soft' collision feedback, and proximity. It also comes with with a suite of examples and prefabs to power reliable, stable 3D user interfaces as well as any physics-critical experiences.

- We include a scene that shows hand tracking working with complex shapes, allowing the user to pick up and interact with objects in the scene
- We have an example to show how to interact with Unity UI
- We include an example showing UI attached to the hand (as opposed to fixed in the scene)

### Hands

Enables developers to use hand tracking data to drive their own 3D Hand assets without writing any code, includes sample hand assets. Can be used to include any custom hand visuals or bind hand tracking data to things in your scene.

- We provide different styles of 3D hands that you can use
- We have in-depth documentation online with an explanation of each feature
- We have included step by step guides within the Editor which teaches you how to set up hands without the need to open online documentation
- No programming knowledge is needed  
- We provide shaders to support HDRP/URP & the Standard render pipeline.

## Preview Features

Our Tracking Preview package gives you early access to additional functionality we're currently working on at Ultraleap. These features may not be ready for our main Tracking package yet - we're open to feedback!

- UI Input
  - Enables the ability to easily retrofit Unity canvases for support of direct and/or indirect hand interaction.
- XR Controller Support
  - Allows XR controller input to drive our hand data, and unlocks the ability to seamlessly swap between controller data and real hands.
- XR Interaction Toolkit Integration
  - Provides support for Ultraleap hand data in Unity's XR Interaction Toolkit.
- Android Support (for SVR)
  - Support for Qualcomm's legacy SVR package for XR2 devices, soon to be deprecated by OpenXR.

## Contributing

Our vision is to make it as easy as possible to design the best user experience for hand tracking use cases in XR. We learn and are inspired by the creations from our open source community - any contributions you make are greatly appreciated.

1. Fork the Project
2. Create your Feature Branch:  
    git checkout -b feature/AmazingFeature
3. Commit your Changes:  
    git commit -m "Add some AmazingFeature"
4. Push to the Branch:  
    git push origin feature/AmazingFeature
5. Open a Pull Request

## License

Use of Ultraleap's Unity Plugin is subject to the [Apache V2 License Agreement][apache].

## Contact

User Support: support@ultraleap.com

## Community Support

Our [Developer Forum][developer-forum] is a place where you are actively encouraged to share your questions, insights, ideas, feature requests and projects.

## Links

[Ultraleap Unity Plugin][repository-url]
