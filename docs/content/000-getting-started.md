# Getting Started {#mainpage}

UnityModules packages from our developer website support Unity 5.6.2, 2017.1-4, and 2018.1.

The UnityModules repository, however, expects Unity 2017.3 and up. If you are sourcing UnityModules directly from this repository, your mileage may vary with earlier versions of Unity.

# Installing the modules {#installing-unity-modules}

Core is the only dependency for any Module. If you are installing any Unity Modules for a new project, you need only download the latest stable versions of Core and any modules of interest from [our developer page][devpage]. You can import new modules into your project at any time; to upgrade a module, simply delete its folder from your project to get rid of all of the files in that module, then import the latest module version.

This documentation is modified as new packages are released, and always reflects the latest released packages on the developer download page.

For more details on upgrading modules, or if your project already contains older versions of our modules, check out [[ our upgrade instructions | Upgrading-Unity-Modules ]] (it's easy).

# Which modules are right for you? {#choosing-modules}

- @ref core

  The dependency for all modules below, Leap Motion's Core Assets provide the foundation for VR applications with a minimal interface between Unity and the Leap Motion Controller and a collection of garbageless C# utilities. With Core, you can [[render a basic set of Leap hands | Core#handmodel-implementations]] or [[attach arbitrary objects to hand joints | Core#attachment-hands]]. Even if you aren't using the Leap Motion Controller, you may enjoy our static Tween utility, Unity object utilities, or Query, our non-allocating LINQ implementation.

- @ref interaction-engine

  Physics representations of hands and VR controllers fine-tuned with interaction heuristics to provide a fully-featured interaction API: grasping, throwing, stable 'soft' collision feedback, and proximity. The Interaction Engine comes with a suite of examples and prefabs to power **reliable, stable 3D user interfaces** as well as any physics-critical experiences akin to Leap Motion's [Blocks][] demo.

- @ref graphic-renderer

  Designed to address the needs of mobile rendering platforms and the ergonomics of personal VR/AR interfaces, the Graphic Renderer allows you to **render an entire curved, 3D, dynamic interface in 1-3 draw calls**. The Graphic Renderer can be used in tandem with the Interaction Engine; see [[this FAQ answer | FAQ-(Graphic-Renderer)#q-can-i-use-the-graphic-renderer-with-the-interaction-engine-if-i-curve-the-graphics-of-interactionbehaviours-like-buttons-and-sliders-will-i-still-be-able-to-interact-with-them]] for a link to our integration example project.

- **[[Hands Module | Hands-Module]]**
    
  The tools you need to rig your 3D hand assets to work with Leap hands, including the powerful [[auto-rigging tool | Hands-Module#autorigging ]].

# Looking for the API docs? TODO: DELETEME MAYBE? {#deleteme}

Unity Modules are generally well-documented! If you're looking for more granular details on Core or any module, don't hesitate to dive into our [Doxygen-generated API documentation for the whole Unity Modules codebase](https://developer.leapmotion.com/documentation/unity/namespaces.html).

# Troubleshooting {#troubleshooting}

If you're not seeing hands in your Unity application:

- If you aren't using a custom camera rig, make sure you have the LMHeadMountedRig (VR rig) prefab or the LeapHandController (desktop-mounted controller) prefab in your scene.

- If you're using a custom camera or controller rig, check your application's scene hierarchy while your hand is in front of the sensor. Make sure the hand isn't simply being rendered in the wrong place relative to your camera. Additionally, double-check you have your rig [[set up correctly | Core#the-core-leap-components]].

- Check the Leap Motion icon in your task bar, which you'll have if you've installed the service from the [developer SDK][devsdk] for your platform. . If it's dark, the Leap service isn't sending any data. Double-check your Leap Motion hardware is plugged in, and check that the Leap Motion service is running.

- Open the Leap Motion Visualizer from the right-click menu of the Leap Motion icon in your task bar. If you see hands in the Visualizer, the problem is mostly likely somewhere in Unity; otherwise, the problem is outside Unity.


[devpage]: https://developer.leapmotion.com/unity/ "Leap Motion Unity Developer Page"
[devforum]: https://community.leapmotion.com/c/development "Leap Motion Developer Forums"
[devsdk]: https://developer.leapmotion.com/get-started/ "Leap Motion Developer SDK"
[Blocks]: https://www.youtube.com/watch?v=oZ_53T2jBGg "Leap Motion Blocks Demo"