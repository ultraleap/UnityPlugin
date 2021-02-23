# (Deprecated) Graphic Renderer {#graphic-renderer}

Deprecation Notice
-------
**This module is deprecated as of October 6th, 2020. It should still function with Core version 4.4.0 and Unity 2019 versions but future support may degrade.**

Introduction
-------

There are a large number of graphical effects that one uses while creating applications.  You might want to allow an amulet to glow with magical runes, or give a button a specific *squish* effect that happens when you press it, or simply change the color of a handle when the user moves their hand close to it.  The implementation of these effects is usually fairly straightforward (relative to the complexity of the effect of course!).  You might use an emissive mask, a special vertex displacement shader, or simply change the color of the material you are using to render the object.

However, most of the time you want to apply these effects to multiple objects, not just one!  *Each* of your handles should change their color when you get close to them, not just one!  Well that isn't too hard, but there is one thing that becomes difficult.  How do I assign different colors to different objects, but still draw them all in one draw call?  I must use a different material for each object, because each object is going to have a different color property.  And when objects have different materials, they are never drawn with the same draw call.  (Note that even when you assign the same material to two different objects, the act of calling renderer.material automatically duplicates the material, resulting in them using different materials and not being drawn in one draw call).

The Leap Graphic Renderer takes a step back and notices a property of this situation.  When we have a bunch of levers, and we want to change the color of them, we are dealing with a situation where *most* of the data shared between the levers are the same.  They use the same shader, they are all normal mapped, they all use the same texture, and they might even use the same mesh!  It seems like a shame that despite all of these similarities, the simple fact that one lever has a different color causes any kind of draw call batching or optimization to be thrown out the window.  Fundamentally, we have a collection of objects that are very similar, with only slight differences between them.

**The Leap Graphic Renderer aims to be a system that allows you to treat a collection of objects as a *group*, instead of as individual and independent objects**.  By treating a collection of objects as a group, we are suddenly able to apply a lot more optimizations and features than we could do by treating them as individuals.

# High Level Concepts/Features {#high-level-concepts-and-features}

The Leap Graphic Renderer:
 * Describes an abstract concept of a graphic as something that you can see.
 * Allows you to organize graphics into groups based on their similarities.
 * Allows you to add modular features to your graphics, based on what you want them to be able to do.
 * Allows you to choose a strategy to render your graphics based on what kinds of optimizations/features you need.
 * Allows you to take advantage of the global nature of the data to apply global effects to every graphic in a group.

That might be a a lot to parse, but we are going to go deep into each section and describe exactly what each means!  First, we will start with the basic concept of what a "Graphic" means to the Leap Graphic Renderer.

The Leap Graphic
----------------

A Leap Graphic defines an abstract concept of something that can be seen by the user.  A leap graphic has no notion of *how* to display itself, it is simply a collection of data and descriptions that describe something that should be visible to the users.  The most obvious example of something that can be displayed is a mesh, and there is a dedicated tree of graphics that are responsible for rendering meshes:

- `LeapMeshGraphicBase`: The base class for defining any graphic that can be represented with triangles/vertices/uvs/ect...
- `LeapMeshGraphic`: The trivial implementation that allows a user to drag in the mesh they want to display.
- `LeapPanelGraphic`:  A procedural mesh graphic that creates 2d panels of variable size and resolution.
- `LeapSpriteGraphic`:  A procedural mesh graphic that allows you to use the mesh of a non-rectangular sprite.

But there is more that you can display besides meshes!  The one other type of graphic packaged into the graphic renderer is the text graphic.  Text is something that can be displayed to the user, and the text graphic describes all of the different settings that one might want to change or control, but again does not concern itself with *how* the text winds up on the screen.

So if graphics do not concern themselves, how do they actually get rendered to the screen?  The answer lies within Rendering Methods!

The Rendering Method
--------------------

Simply put, a rendering method is a piece of code that takes a list of graphics and decides how best to get those objects rendered as pixels on your screen.  A rendering method can take any approach it wants.  There is nothing that says that a rendering method must display a mesh graphic by uploading those vertices and triangles to the gpu.  A voxel rendering method might instead decide to voxelize the mesh first, and then display it using a custom voxel rendering system.  

On a more simplistic note, the main reason for rendering methods is because there are different optimization strategies that one can use to render the same concept, and these strategies have different benefits and trade-offs.  Certain strategies might be unable to support certain features altogether!  There are three rendering methods pre-packaged with the Leap Graphic Renderer:

1) `The Baked Renderer`:  This rendering method is able to draw mesh graphics only!  It's strategy is to bake all of the graphics into a single mesh so that they all can be drawn with a single draw call.  For more information on the baked renderer, check out the Baked Renderer section!

2) `The Dynamic Renderer`:  This rendering method is also only able to draw mesh graphics.  It's strategy is to do some custom mesh manipulation to ensure that all graphics can be drawn with the same material no matter what, and tries to ensure that all graphics can be automatically batched by Unity's dynamic batching.  For more information, check out the Dynamic Renderer section!

3) `The Dynamic Text Renderer`:  This rendering method is only able to draw text graphics.  It's strategy is simply to use Unity's built-in dynamic font generation to create meshes that represent the text that needs to be displayed.  For more information, check out the Text Renderer section!

Note that the power of the rendering method lies within the fact that it can look at *all* graphics at once, which allows it to perform optimizations that would otherwise be difficult (or impossible) to do without that information.

Graphical Features
------------------

Graphics can describe the base representation of an object, but they don't tell the whole story.  What if I want to change the color of an object at runtime, how would one go about that?  The solution is a concept called Graphical Features.  A feature describes a specific functionality that can be added to a graphic to change the way it behaves.  Each feature has the following two pieces:

  1) The feature itself, which is a global concept that affects all objects being rendered by a rendering method.
  2) The per-graphic feature **data**, which is a per-graphic concept that affects only a single graphic.
  
Let's look at a simple example, a texture feature!  A texture feature allows each graphic to have a texture assigned to it.  That texture object is the per-graphic feature **data**.  The texture is assigned directly to a graphic, and only affects the graphic it is assigned to.  The texture **feature** itself will have a global concept of a channel name, which would be something like _MainTex, and a uv channel, which could be something like uv0.  

Here is a basic list of all existing graphical features:
 * `Runtime tint`:  Allows you to change the color of a graphic at runtime.
 * `Texture`: Allows you to assign a texture to a graphic.
 * `Sprite`: Allows you to assign a sprite to a graphic.
 * `Blend Shape`: Allows you to warp the shape of the object at runtime.
 * `Custom Channels`:  Allows you to specify arbitrary data at runtime.  These are more complicated, for more information check out the "Custom Channels" section.

One important thing to note!  Like graphics, a feature is a very abstract concept in that it does not describe *how* the feature is implemented!  It is just a description of an effect that is desired.  A runtime tint feature does not have any notion of shaders or uploading data to the gpu, it is simply a structure to specify that a graphic can have a color assigned to it, that that color can change at runtime, and that this color should apply a multiplicative tint to the color of the object.  Once again, it is the responsibility of the rendering method to decide how best to implement the feature.

The Graphic Renderer component and Graphic Groups
-------------------------------------------------

The Graphic Renderer component is the center for everything graphically related.  It is the 'manager' component if you will, and controls all graphics that are childed to it.  Each Graphic Renderer can have a number of different Groups.  For a graphic to be displayed, it must be attached to a group.  A group has the following information:

 - The rendering method used to display all attached groups.
 - Allows you to configure the rendering method.
 - The features to be attached to the graphics that are a part of this group.
 - Allows you to configure the attached features.

You can have as many groups as you want, and each group can have a different feature set and rendering method.  This is to allow different types of graphics to easily live in the same hierarchy next to each other.  A simple example is when you want to have a panel with text on it.  The panel could be rendered using a mesh graphic rendered using a baked renderer, and the text could be rendered using a text graphic attached to a text renderer.

A simple interface on the graphic itself can show you if a graphic is currently attached to a group, and allow you to easily change which group it is attached to.  You can give each group a different name to make it easier to find and locate which group you want.

# Getting Started {#getting-started}

This getting started document aims to give you a basic understanding of how to use the graphic renderer.  To start, you want to create a new empty gameObject in your hierarchy, and attach the LeapGraphicRenderer component onto it.  The LeapGraphicRenderer is the main component that will manage all graphics that are attached to it.  Press the 'New Group' button and create a new Baked group.  This will create a new group that is capable of rendering graphics!  Because we selected the 'Baked' option when creating a new group, this group will use the Baked rendering method.  The group will show you all of the different options of the rendering method, as well as a list at the bottom to add features:

![][Tutorial_CreatingBakedRenderer]

Now that we have a group, let's add some graphics to render!  Create a new empty gameObject as a child of the renderer object.  Then, add the LeapMeshGraphic component to it.  Assign a mesh (I like the cube to start!) to the mesh field.  Right away, you should see the cube appear in the scene view and the game view!

![][Tutorial_AddCubeMeshGraphic]

You will notice that the cube is pure white!  You can change the vertex color of the mesh to tint it a certain color, but otherwise it remains a solid color.  This is because all renderers default to using an unlit shader, which does not interact with the lighting in unity.

Head back to the graphic renderer and notice the section at the bottom of the interface named `Graphic Features`.  Press the `+` button to add a new feature, and select the `Texture` option from the dropdown.  You will see a new element has been added to the list!

![][Tutorial_AddingATextureFeature]

We have just given our graphics the ability to wrap textures onto their surface.  Head back to the graphic and notice that a property field has appeared that allows you to assign a texture to it.  Go ahead and choose a texture you want to assign to your mesh.  Once you do, you should see a button appear that says `Refresh Atlas`.  The same button exists on the graphic renderer as well.  Pressing this button updates all of the textures used by the graphic renderer, which is necessary to observe any texture-related changes.  Press the button now and see your texture mapped onto the graphic!

![][Tutorial_AddingATexture]

At this point, you can create a bunch of different mesh graphics, each with a different mesh and a different texture.  

![][Tutorial_LotsOfGraphics]

The power of the graphic renderer, is that all of these objects are all drawn in *a single draw call*.  The renderer automatically combines the meshes into a single mesh, and atlases the textures so that they can all be drawn at once!  The workflow for you remains focused on graphics as an individual entity however, instead of part of a group, which allows for a much more intuitive workflow.

Now, you might have noticed that when you have the vertex colors to have a non-opaque alpha, or when using textures that have transparent portions, that they do not render as transparent.  This is due to a specific limitation of the baked rendering method.  However, there is another rendering method besides baked!

Head back to your graphic renderer and click the `New Group` button again, and this time select the `Dynamic` option.  This will create an entirely new group!  This group is *separate* from the baked renderer, and as you can see, it has no elements attached to it yet!

![][Tutorial_CreateDynamicRenderer]

Head over to one of your graphics, and notice the button in the top right of the script.  It should say baked currently.  Click on that button and select Dynamic from the dropdown.  This action changes which group that graphic is attached to, from the baked group to the dynamic group.  You may need to press the 'Refresh Atlas' button again, but once you do, you should see transparency effects begin to work for that object!

![][Tutorial_MoveToDynamicGroup]

This shows how it is possible for there to be multiple different rendering groups, even within the same hierarchy.  This allows you to group graphics by which features they need, whether that be textures, vertex colors, or transparency!

[Tutorial_CreatingBakedRenderer]: http://blog.leapmotion.com/wp-content/uploads/2017/06/Tutorial_CreatingBakedRenderer.gif
[Tutorial_AddCubeMeshGraphic]: http://blog.leapmotion.com/wp-content/uploads/2017/06/Tutorial_AddCubeMeshGraphic.gif
[Tutorial_AddingATextureFeature]: http://blog.leapmotion.com/wp-content/uploads/2017/06/Tutorial_AddingATextureFeature.gif
[Tutorial_AddingATexture]: http://blog.leapmotion.com/wp-content/uploads/2017/06/Tutorial_AddingATexture.gif
[Tutorial_LotsOfGraphics]: http://blog.leapmotion.com/wp-content/uploads/2017/06/Tutorial_LotsOfGraphics.gif
[Tutorial_CreateDynamicRenderer]: http://blog.leapmotion.com/wp-content/uploads/2017/06/Tutorial_CreateDynamicRenderer.gif
[Tutorial_MoveToDynamicGroup]: http://blog.leapmotion.com/wp-content/uploads/2017/06/Tutorial_MoveToDynamicGroup.gif

# The Leap Graphic {#the-leap-graphic}

The Leap Graphic is the base class for everything that can be displayed in the Graphic Renderer system.  The Leap Graphic class is abstract and contains no actual display information itself.  It is mostly focused on lifecycle and management systems such as:

 - Which group the graphic is currently attached to (if any)
 - All feature data currently attached to the graphic.
 - What the preferred rendering type of the graphic is.
 - What space anchor the graphic is attached to (if any)
 - What space transformer should be used to transform this object (if any)

For more information about spaces/space anchors/space transformers, visit the **Curved Space Section**.

The Leap Graphic class also provides you with callbacks that you can hook into to be able to know whenever:

 - The graphic is attached to a group.
 - The graphic is detached from a group.

Leap Mesh Graphic Base
-------------
The Mesh Graphic Base is the abstract base class for all graphics that represent themselves with a mesh object.  It provides a very simple interface to provide mesh data to whatever rendering method is trying to render it.  Each of these values is a property that can be assigned to by an implementing class to create different behavior.

 - `vertex color` - A color that can tint the existing vertex colors of the provided mesh.  If the mesh has no vertex colors at all, this color can be used as a substitute.
 - `mesh` - The mesh object that represents this graphic.  It must use triangle topology, and can contain uvs, colors, or normals in addition to its normal vertex/triangle information.
 - `remappableChannels` - Used only for texture atlasing.  When atlasing a texture the uv channels are automatically remapped to point to the correct location in the atlas.  Only uv channels present in this mask will be remapped.

There is a single abstract method, named RefreshMeshData.  This method is called by a rendering method to make sure that the vertex color, mesh, and remappable channel properties are up to date and correctly reflect the state of the graphic.  RefreshMeshData would be the callback to implement if you wanted to create a procedural mesh graphic.

Leap Mesh Graphic
----------
The LeapMeshGraphic component is the trivial implementation of the LeapMeshGraphicBase api.  It simply provides a field for each property to allow the user to directly set the values themselves.  You can drag your own mesh into the mesh slot, and directly specify which uv channels you want to allow to be remapped.

The usage of LeapMeshGraphic closely mirrors the usage of the MeshFilter/MeshRenderer workflow in the normal Unity rendering system.

Leap Panel Graphic
----------
The LeapPanelGraphic component is a simple example of a procedural mesh graphic implementation of the LeapMeshGraphicBase api.  The graphic describes a 2D panel of an arbitrary dimension and resolution.  The panel also supports nine slicing if there is a sprite feature attached to this graphic.

The Panel Graphic has two different ways you can specify the resolution of the panel:
 - `Vertices mode` - The user can specify the exact number of extra vertices to use in the creation of this panel.  Horizontal and vertical resolution can be specified independantly.
 - `VerticesPerRectilinearMeter mode` - The user can specify a number of vertices to use as a function of the dimensions of the panel.  This option makes it easy to provide a constant density of vertices that is independent from the dimension of the panel.

The panel also supports being attached to a gameObject with a RectTransform, in which case its dimension will be driven by the rect transform instead of by an explicit size property. 

Leap Sprite Graphic
-----------
The LeapSpriteGraphic is another simple example of a procedural mesh graphic implementation.  The sprite graphic simply uses the custom sprite mesh from an attached sprite feature.  This allows the user to easilly take advantage of non-rectangular sprite meshes automatically.

Leap Text Graphic
----------
The LeapTextGraphic is the only graphic included in the default Graphic Renderer package that is not a mesh graphic.  Currently the dynamic text renderer is the only rendering method that is capable of drawing the leap text graphic.

The leap text graphic is designed to closely mirror the usage of the built-in Text component in the unity UGUI system.  It contains all of the basic properties needed to display text in a 2d plane:

 - `Text` - The text string itself that should be displayed.
 - `Font Style` - The style of font that the text should be displayed with.
 - `Font Size` - The size of font that should be used to display this text.
 - `Line Spacing` - The amount of space that should be between each horizontal line.  By default, the value is 1, which is an abstract value that is not related to actual world units.
 - `Horizontal Alignment` - The horizontal alignment of the text, which can either be left, center, or right alignment.
 - `Vertical Alignment` - The vertical alignment of the text, which can either be bottom, middle, or top.
 - `Color` - The color of the text, which is black by default.

# Graphical Features {#graphical-features}
 
A graphic feature is a concept that cannot be rendered by itself, but is a description of an effect or modification that can be applied to an existing graphic.  A simple example of a feature is the ability to apply a tint to an object at runtime.  The concept of features allows you to selectively describe what you want your graphics to be able to do, and then let the rendering method optimize for the set of features that you have chosen.

A Graphical Feature is broken down into two distinct parts, the Feature itself, and Feature Data.  The Feature object is added to a rendering group, where global settings can be configured.  Feature data is added directly to a graphic, and controls per-graphic settings.

Texture Feature
---------------
The texture feature allows you to add textures to your graphics.  A texture feature has the following settings:

 - `Property` - The name that identifies this texture feature.  This is also the name of the material property that represents this texture.  By default, the name is _MainTex, which matches the default texture property name in Unity materials.
 - `Channel` - The uv channel that the texture will be tied to.  Must be a channel from 0 to 3.  This defines the connection between the texture and the uv coordinates located on the mesh it is being applied to.

Textures can be automatically atlased by the rendering method, check out the section on Atlasing for more information.
 
Sprite Feature
--------------
**Note** There is currently a bug in the Unity editor api that causes sprite data to be incorrect read under certain conditions.  Make sure you enter/exit play mode a few times if sprite features are not showing up correctly.

The sprite feature is very similar to the texture feature, and allows you to add sprites to your graphics.  The sprite has the same settings as the texture feature:

- `Property` - The name that identifies this sprite feature.  This is also the name of the material property that represents this sprite.  By default, the name is _MainTex, which matches the default texture property name in Unity materials.
- `Channel` - The uv channel that the sprite texture will be tied to.  Must be a channel from 0 to 3.  This defines the connection between the texture and the uv coordinates located on the mesh it is being applied to.

The Sprite feature has a number of specific advantages over the Texture feature:
 - It is able to provide extra sprite-specific data.  This can enable features like nine slicing, or non-rectangular image generation.
 - Sprites do not need to be packed by the special tool, letting you use the existing sprite packing workflow that you are familiar with.

The Sprite feature has the limitation that *all* sprites must exist on the *same* atlas in order to be used by a renderer.

Runtime Tint
------------
The Runtime Tint feature allows you to dynamically change the color of an object at runtime.  Even if you are using a mesh graphic with vertex colors enabled, you will not be able to change the color at runtime without using a runtime tint feature.  This feature does not have any specific renderer-wide settings, it simply allows you to specify a tint for each graphic.  This has the following effects for the built-in shaders:

 - `Built in unlit shaders` - The object's color is multiplied by the tint color.
 - `Built in surface shaders` - The object's Albedo is multiplied by the tint color.

Blend Shape
-----------
The Blend Shape feature allows you to blend between two different shapes when using a mesh graphic.  This feature can be very useful when you want to provide small animations to your graphics, such as a stretch or squash.  It can also be useful if you want to have your graphics undergo very subtle translations/rotations/scales but you don't want to eat the cost of supporting full movement for every graphic.  Like the runtime tint, there are no global settings, everything you want to configure is per-graphic:

 - `Amount` - The amount to blend from the default shape to the alternate shape.  Should be a value that ranges from 0 (default shape) to 1 (alternate shape).
 - `Type` - The type of shape to use for the alternate shape.  Allows you to choose between a few useful types:
   - `Translation` - Auto-generate the alternate shape based on a translation of the default shape.
   - `Rotation` - Auto-generate the alternate shape based on a rotation of the default shape.
   - `Scale` - Auto-generate the alternate shape based on a translation of the default shape.
   - `Transform` - Auto-generate the alternate shape by applying a full transformation to the default shape.
   - `Mesh` - Use a blend shape delta specified in the default shape.  (Usually created by a modeling program like Blender/Maya) 
 - `Translation` - If the type is set to Translation, this property specifies the amount to translate.
 - `Rotation` - If the type is set to Rotation, this property specifies the amount to rotate.
 - `Scale` - If the type is set to Scale, this property specifies the amount to scale.
 - `Transform` - If the type is set to Transform, this property specifies the position/rotation/scale to transform to.

Custom Channels
---------------
A Custom Channel is an advanced feature, and is only useful when you are writing your own custom shaders.  It allows you to upload custom per-graphic data into the GPU that you can access and use for your own effects.  The custom channel comes in 4 flavors, each defined by the data type they allow you to upload into the GPU:

 - `CustomFloatChannel` - Upload one float per graphic to the gpu.
 - `CustomVectorChannel` - Upload one float4 per graphic to the gpu.
 - `CustomColorChannel` - Upload one float4 per graphic to the gpu, but provide a nice color inspector on the graphic.
 - `CustomMatrixChannel` - Upload one float4x4 per graphic to the gpu.

Each channel must specify it's property name, which must match the material property used in the shader.  Custom properties are defined and accessed in a special way.  Instead of writing:

	uniform float _MyChannel;
    
You would write:

	DEFINE_FLOAT_CHANNEL(_MyChannel);
    
You must access the custom channel in the vertex shader.  Instead of writing something like:

	vertOut.value = _MyChannel;
    
You would instead write:

	vertOut.value = getChannel(_MyChannel);
    
Accessing And Using Feature Data Objects
------------------------------
When you want to access or use a feature data object, you have a few API's you can choose from.  You can always simply use the LeapGraphic.featureData list to iterate through all feature data objects attached to a given graphic, but there are also several utility methods provided for ease of use:

 - `GetFeatureData<T>` - Gets the *first* feature data object of type T and returns it.  If there is no feature data object of type T the method will return null.
 - `Get/SetRuntimeTint` - Gets or sets the runtime tint.  Internally this just calls GetFeatureData.
 - `Get/SetBlendShapeAmount` - Gets or sets the blend shape amount.  Internally this just calls GetFeatureData.
 - `GetCustomChannel` - Gets a custom channel object by name.  Internally just loops through the featureData list.
 - `SetCustomChannel` - Sets a custom channel value by name.  Internally this just calls GetCustomChannel.

It is **highly** recomended that if performance is a concern that you always use GetFeatureData or GetCustomChannel inside of an initialization callback and store a reference to the returned feature data object.  The mentality is much like the usage of GetComponent in that it is always a good idea to cache your references.

The other gotcha is that Get/SetCustomChannel do not function if the graphic is not currently attached to a group.  The reason for this is because the actual name of the channel is stored in the group, not the graphic!  So the name of a channel is actually not known until the graphic becomes attached. 
 
# Rendering Methods {#rendering-methods}
 
A Rendering Method is a body of code that takes in a list of graphics, the features connected to those graphics, and performs some action to display those graphics to the user.  The specific implementation is completely up to the decision of the rendering method.  As such, the API for a rendering method is extremely simple:

 - A rendering method gets callbacks for OnEnable/OnUpdate/OnDisable during both runtime and edit time.
 - A rendering method can get access to the group it is attached to, which contains the list of graphics to display and the features that are attached.

With these two things, the rendering method must decide how to go about displaying the graphics to the screen.  For example, a very simple rendering method that wanted to display a mesh graphic might simply loop through each graphic and call Graphics.DrawMesh() using the mesh obtained from the graphic.  There are three rendering methods packaged with the graphic renderer module:

 - Baked Renderer
 - Dynamic Renderer
 - Dynamic Text Renderer

They all have different advantages/disadvantages, as well as different types of graphics and features they support.

Support Info
------------
A rendering method is under no obligation to implement support for any feature or graphic type.  In fact, a renderer must explicitly specify which features, graphics, and actions it supports.  The relevant interfaces are detailed here:

 - `ISupportsFeature<T>` - A renderer must implement this interface in order to specify that it is able to render a specific feature.  The interface also includes a method so that a renderer can conditionally enable or disable support.  For example, a renderer might support a *single* texture feature, but no more than that.
 - `ISupportsAddRemove` - A renderer must implement this interface in order to specify that it supports having graphics added and removed from it at runtime.  If this interface is not implemented, you will be unable to add or remove graphics from a group that uses that rendering method.
 - `IsValidGraphic<T>` - A renderer must implement this abstract method to specify whether or not a given graphic is a valid graphic type for this rendering method.  Note that this function takes no arguments, and so a graphic is not able to be conditionally accepted or rejected from a graphic based on conditions other than its type.

Mesh Renderers
--------------
There are two rendering methods that support rendering mesh graphics.  They are the baked renderer, and the dynamic renderer.  Even though they are different renderers, they share a lot of the same features:

 - You can specify which mesh attributes are included in your mesh.  This includes the uv channels 0 to 3, vertex normal, and vertex colors.
 - If you specify vertex colors, you can also specify a global tint that is applied to all of the vertex colors in the entire group.
 - You can specify the visual layer that the graphics are displayed on.  **NOTE** that all graphics will be displayed on that layer, regardless of what the layer of the gameObject the graphic is on actually is.  Conceptually you can think of this as the visual representation being separated from the game object representation.
 - You can specify the atlas settings used.  For more information, visit the **atlasing** section.

Baked Renderer
--------------
The Baked Renderer is one of two pre-packaged renderers that supports rendering mesh graphics.  It is the sibling component to the Dynamic Renderer.  The baked renderer has a number of specific advantages:

 - All graphics attached to the group are all drawn together in a *single draw call*.
 - No runtime overhead for dynamic batching or runtime mesh generation.
 - Supports optimizations based on whether or not you expect your graphics to move around or not.
 - Supports creating a 'proxy' game object that hosts the single mesh used to display the graphics, rather than drawing the meshes with Graphics.DrawMesh().

The baked renderer also has a number of specific limitations:
 - Currently runtime rotation or scaling is not supported by the renderer, only translation.
 - Runtime sorting of graphics relative to each other is not supported, and so transparent shaders might show artifacts in specific cases.
 - Does *not* support adding or removing graphics at runtime.
 - Does not support culling graphics on a per-graphic basis.  All graphics are always drawn.

Internally, the baked renderer functions by baking all of the graphics into a single mesh, which can always be drawn with a single draw call.  This is a very efficient way to render a group of graphics if you can be sure that the general form of the group is not changing at runtime.

Dynamic Renderer
----------------
The Dynamic Renderer is the second of the two pre-packaged renderers that supports rendering mesh graphics.  It is the sibling component to the Baked Renderer.  The dynamic renderer has a number of specific advantages:

 - All graphics attached to the group are all drawn together with the same *material*.  This gives the objects the best possible chance to be dynamically batched together, but other scene settings might still prevent them from being batched.
 - Full translation/rotation/scaling is always supported.
 - Adding and removing graphics at runtime is supported.
 - Changing the mesh of a graphic is supported (although it does require a rebake of the mesh, which is not the cheapest)
 - Graphics are correctly sorted to allow for transparency effects.
 - Graphics are correctly culled on a per-graphic case.

The dynamic renderer also has a number of specific limitations:
 - It is a lot harder to guarantee that the entire group is drawn in one draw call due to the difficulty of predicting when dynamic batching will take effect.
 - It requires a more expensive shader than the Baked Renderer.

The dynamic renderer can be considered the 'fallback' from the Baked Renderer due to it's higher cost.  If possible, you should always try to use the baked renderer, but if you need a feature from the dynamic renderer, it can be easy to switch.  Also always remember that if you only need a feature for a single graphic in your scene, you can always use a different group for that single graphic.

Atlasing
--------
Both the dynamic and baked rendering methods utilize an atlasing strategy to allow each graphic to have an arbitrary number of texture features attached to it.  When multiple graphics each use different textures, all of the textures are copied into an 'atlas', which is a large texture which contains multiple textures inside of it in different places.  Each graphic has its uvs remapped to point into this atlas instead of the original texture, giving the appearance that each graphic is using a different texture.

There will always be a limit to the number of textures you can include in a graphic group because of the limit to how big an atlas can be.  The maximum texture size supported in unity is 8192 (and it is smaller on some platforms!) and so the total amount of texture data cannot exceed 8192x8192 pixels.

Since atlas generation can be an expensive process, it is not re-generated automatically.  When using a rendering method that supports atlasing, you will see a little button pop up in the inspector called 'Refresh Atlas', which will trigger a rebuild of all atlases.  This button will turn yellow if the system has detected that the atlas is not up-to-date.  A button will also appear on the graphics themselves that will allow you to refresh the atlas without needing to go visit the renderer component.

Each renderer that supports atlasing will have a specific section where you can specify different settings that control how the atlas is built:

 - `Border` - How much pixels of empty space to put between each texture when they are placed into the atlas.  The empty space makes not guarantees about what color it is!
 - `Padding` - How many pixels each texture should be expanded by before being placed into the atlas.  When a texture is padded, extra pixels are chosen based on the wrap mode of that particular texture.  Useful when you are seeing seams due to filtering around the edges of your uvs.
 - `Mip map` - Should the atlas also contain mip maps?  These are recalculated when the atlas is generated and not based on the mip maps of the textures themselves.
 - `Filter mode` - What the filter mode of the atlas texture should be.  This will affect all graphics!
 - `Format` - The format the atlas should be stored in.  Uncompressed RGBA by default.
 - `Max atlas size` - The maximum size the atlas can be in pixels.  You can lower this to cause automatic downscaling!
 - `Extra textures` - You can place textures into this array to force them to always be included in the atlas, even if they are not referenced by any graphic.  Useful when spawning in graphics at runtime.

**Note** that an atlas cannot be built at runtime, so even if a renderer supports adding and removing graphics at runtime, if a texture referenced by a spawned graphic is not present in the atlas, it will not display properly!  You can fix this issue by using the 'Extra textures' field in the atlas settings to force the texture to always be included in the atlas.

Dynamic Text Renderer
---------------------
The Dynamic Text Renderer is the only included rendering method that is able to render text components.  It is a fairly simple wrapper around the built-in dynamic font system that unity has, and uses strategies similar to the dynamic renderer to render the generated font meshes.  Because it uses the built-in font system, it is limited to only displaying fonts that are supported by Unity's system, and can't handle any kind of special effects like outlines or bevel.  The dynamic text renderer also currently has no support for any additional features.

The renderer has a number of settings that control how the text is viewed:

 - `Font` - All graphics that are attached to this renderer all use this same font.
 - `Dynamic Pixels Per Unit`- Changing this value will change the resolution of the texture used for each character without actually changing the size of the text.  Turn this up if your text looks too blurry, and turn it down if they look too sharp or aliased.
 - `Use color` - Should the internal generated mesh contain vertex colors?  Will use the colors set on the text graphic.
 - `Scale` - You can use this to add a scaling factor to every text graphic attached.  This is not the same as scaling the transform, as it still obeys wrapping and layout rules.
 
# Curved Spaces {#curved-spaces}
 
Leap Spaces provide generic ways to define non-rigid warpings to objects in a hierarchy without actually moving and rotating the objects to try to get them to fit.  This can be particularly useful when you want to define a curved space for an interface and you want to be able to easily tune or modify the shape of the interface as a whole.  When dealing with curved spaces, all of your elements will remain in rectilinear space, and the space component allows you to access information about where they should *appear* to be correctly warped by the space.

The LeapSpace class it he base class that defines all types of spaces.  Currently the only included spaces are the cylindrical space and the spherical space.  The space class is designed for high performance situations, and so its API is slightly different to use.  You must always call `RebuildHierarchy` in order for the space component to become aware of creations, deletions, or other hierarchy changes that have occurred.  Once that has been rebuilt, you must call `RecalculateTransformers` to recalculate the information used to warp the objects.  At any point that an object changes its position/rotation/scale you need to call RecalculateTransformers before you can get an accurate view of where it should be warped to.  This API is designed to handle a large number of objects at once, and so for best results you should do all of your warping calculations in one place, at most once per frame.

The space component works off of behaviors that implement the `ISpaceComponent` interface.  A space component is simple an object that can be part of a space.  All space components measure their transformation into the space relative to an `anchor` they are attached to, and so the only interface provided is a getter for the attached anchor.

Anchors are concepts that are a fundamental part of the space api.  All space transformations are measured relative to an anchor.  By default, the space component is the only anchor in a hierarchy, but you can add the `LeapSpaceAnchor` component to an object to create a new space anchor that affects all space components that on, or are a child of that anchor.  The effects of an anchor are slightly difficult to explain, but can be thought of as a 're-propagation' of the space.  The anchor causes all child space components to treat the space as if the *anchor* is the center, rather than the actual space component itself.  This is mostly used to preserve sizes and make sure that distortion can be controlled.

Here we have a simple box as part of a cylindrical space.  We can see that as we move it backwards, it gets a significant stretch along the X axis due to the cylindrical warping.  

![][CurvedSpaces_01]

This can be undesirable in certain situations.  If we add a space anchor to the box, we can see that the object is still warped by the space, but because of the relative warping of the anchor, the box does not stretch.

![][CurvedSpaces_02]

Here we have another example.  There are three boxes that are all part of a cylindrical space.  As the parent of the boxes is moved along the Z axis, the boxes all move along the radial direction. 

![][CurvedSpaces_03]

If we add a space anchor to the parent, we can see that the boxes still move outwards, but now maintain their horizontal relative positions to each other thanks to the new anchor.

![][CurvedSpaces_04]

Rectilinear Space
-----------------
[Rectilinear]() space is the space you are used to working with in Unity.  When an object moves along the x,y, or z axis, it travels in a straight line at a constant rate.  When working with spaces, all of your objects will remain in this rectilinear space and you can move them in the exact same ways you have always moved them.  It is the space component that tells you where your object should *appear* based on its rectilinear position.

Cylindrical Space
-----------------
![][CurvedSpace_Cylindrical]

The cylindrical space component is one of the two space components that comes pre-packaged in UnityModules.  This component can be considered to "re-map* the X, Y and Z components of the objects rectilinear positions:

 - `X` - Becomes the *angle* around the vertical axis that the object is rotated.
 - `Y` - Is unaffected, this still moves the object in a straight line up and down.
 - `Z` - Becomes the *radius* away from the center axis that the object is placed.

When using a cylindrical space, if you move an object along the X axis in rectilinear space, it will rotate in a circle in curved space.  If you move an object along the Z axis in rectilinear space, it will move along the radial axis relative to the center of the space.

Spherical Space
---------------
![][CurvedSpace_Spherical]

The spherical space component is the other pre-packaged space component in UnityModules.  This component is very similar to the cylindrical space component, but instead of mapping the rectilinear space onto a cylinder, it maps it onto a sphere:

 - `X` - Becomes the angle around the vertical axis that defines a point on a sphere (using Euler angles)
 - `Y` - Becomes the angle around the horizontal axis that defines a point on a sphere (using Euler angles)
 - `Z` - Becomes the radius from the center that defines a point on a sphere.

Unlike the cylindrical space, the spherical space has singularities at the north and south poles where your object can be squished into a single point, or highly distorted.  See [Map Projection]() for why these singularities and distortions are unavoidable when trying to transform a rectilinear space into a spherical space.

[CurvedSpaces_01]: http://blog.leapmotion.com/wp-content/uploads/2017/06/CurvedSpaces_01.gif
[CurvedSpaces_02]: http://blog.leapmotion.com/wp-content/uploads/2017/06/CurvedSpaces_02.gif
[CurvedSpaces_03]: http://blog.leapmotion.com/wp-content/uploads/2017/06/CurvedSpaces_03.gif
[CurvedSpaces_04]: http://blog.leapmotion.com/wp-content/uploads/2017/06/CurvedSpaces_04.gif
[Rectilinear]: https://en.wikipedia.org/wiki/Regular_grid#Rectilinear_grid
[Map Projection]: https://en.wikipedia.org/wiki/Map_projection
[CurvedSpace_Cylindrical]: http://blog.leapmotion.com/wp-content/uploads/2017/06/CurvedSpaces_Cylindrical.gif
[CurvedSpace_Spherical]: http://blog.leapmotion.com/wp-content/uploads/2017/06/CurvedSpaces_Spherical.gif 
 
# Preferences {#preferences}
 
The Graphic Renderer module has some global preferences that can be configured by visiting the [preferences][] menu.  You will see a tab named `Leap Graphics` near the bottom, which should look something like this:

![leap graphics preferences][]

 - `Max Graphics Per-Group` - Describes the maximum number of graphics that can be attached to a group at once.  This limit is for shader purposes only, and is not a CPU constraint.  The number is set at 48 to start because different graphics apis support different number of graphics.  The maximum count also can depend on the features you are using, sometimes adding more features can remove the total number of graphics you can support.  Currently the best way to find out what the maximum count should be for your application is to just experiment with different values and see what works.  It's important to note that changing this value will cause a re-import of all shaders, so if you have any surface shaders with #ref shader-variants you might experience some long import times!
 - `Prompt when group changed` - Determines whether or not the system should warn the user when they are switching a graphic from one group to another with a different feature set.  When the target group has a different feature set, it can result in data being lost as the graphic is re-configured to have the correct set of feature data objects, which can be frustrating if that is not intended.
 - `Prompt for custom channel` - Determines whether or not the system should warn the user when they try to add a custom channel feature.  Custom channels require @ref writing-your-own-shaders and so are not for the average user.  
 - `Enable/Disable all variants` - Allows the user to globally enable or disable all variants for graphic renderer surface shaders.  For more information on variants, visit the @ref shader-variants section.

[preferences]: https://docs.unity3d.com/Manual/Preferences.html
[leap graphics preferences]: http://blog.leapmotion.com/wp-content/uploads/2017/06/GraphicRendererPreferences.jpg
 
# Shader Variants {#shader-variants}
 
Shader variants are a Unity-specific feature that allows features to easily be turned on and off without writing a completely new shader.   They have some excellent [documentation on shader variants in the manual][] that goes into detail about what they are and why they are useful.  When using the graphic renderer, all of the default shaders use variants to allow different features to be enabled and disabled.  The included variants are:

 - `GRAPHIC_RENDERER_CYLINDRICAL` - Enabled when the shader will be displaying objects in a cylindrical space.
 - `GRAPHIC_RENDERER_SPHERICAL` - Enabled when the shader will be displaying objects in a spherical space.
 - `GRAPHIC_RENDERER_VERTEX_NORMALS` - Enabled when the mesh object has vertex normals.
 - `GRAPHIC_RENDERER_VERTEX_UV_0` - Enabled when the mesh object has texture coordinates in the 0 channel.
 - `GRAPHIC_RENDERER_VERTEX_UV_1` - Enabled when the mesh object has texture coordinates in the 1 channel.
 - `GRAPHIC_RENDERER_VERTEX_UV_2` - Enabled when the mesh object has texture coordinates in the 2 channel.
 - `GRAPHIC_RENDERER_VERTEX_UV_3` - Enabled when the mesh object has texture coordinates in the 3 channel.
 - `GRAPHIC_RENDERER_VERTEX_COLORS` - Enabled when the mesh object has vertex colors.
 - `GRAPHIC_RENDERER_MOVEMENT_TRANSLATION` - (Baked renderer only) Enabled when the baked renderer wants to support translation.
 - `GRAPHIC_RENDERER_TINTING` - Enabled when the shader will be displaying runtime tinting for the objects.
 - `GRAPHIC_RENDERER_BLEND_SHAPES` - Enabled when the shader will be displaying blend shapes for the objects.
 - `GRAPHIC_RENDERER_ENABLE_CUSTOM_CHANNELS` - Enabled whenever the shader is using any custom channels.

These variants are automatically enabled and disabled by the rendering methods, so you usually don't need to worry about them.  But there is a single situation where things are slightly different.

How Surface Shaders are Different
---------------------------------

[Surface shaders][] are treated slightly differently by the graphic renderer in a few ways.  They *always* have the following variants enabled (and if you create your own custom shaders you need to make sure you always include them!): 

 - `GRAPHIC_RENDERER_VERTEX_NORMALS` - Surface shaders always need normals
 - `GRAPHIC_RENDERER_VERTEX_UV_0` - Surface shaders always need a texture channel

For more information, you can check out the section on @ref writing-your-own-shaders.  The other important difference is that due to a Unity bug, and slow surface shader import, we have been forced to *disable* variants for surface shaders.  Importing a surface shader with all of the above variants can take up to 3 minutes even on a powerful computer, which would cause the import time for the Graphic Renderer Package to be terrible, due to it including a number of surface shader examples/defaults.

We have opted for a simple workaround.  Variants have been disable for all of the included surface shaders, but they can be enabled by the user if they want to use them!  This allows the large import time to be on the users terms, and is a lot more expected due to the controlled environment.  There are two ways to enable variants for a surface shader.  The first way is simple, just drag a surface shader into the shader slot of a rendering method, and you should see a warning dialog pop up that looks something like this:

![shader variant warning][]

When you press the button, it will first warn you of the long import times, and then *permanently enable variants* for that surface shader.  The long import time will only be experienced again if the shader needs to be imported again, which can happen when switching platforms, or when modifying the shader.  The other way to enable shader variants is to visit the @ref preferences, where you will have more options for enabling and disabling variants.

Getting around the import issue
-------------------------------

The automatic shader variant enabling/disabling can be quite annoying even with the automatic enable/disable feature, especially if you are constantly in a situation where you need to re-import the shader.  The only way to get around the import time issue is unfortunately to remove the concept of variants all together by @ref writing-your-own-shaders.  You can write your shaders and manually enable or disable the variants you choose.  Just make sure that the features you have enabled on your graphic renderer match up!

[documentation on shader variants in the manual]: https://docs.unity3d.com/Manual/SL-MultipleProgramVariants.html
[Surface shaders]: https://docs.unity3d.com/Manual/SL-SurfaceShaders.html
[shader variant warning]: http://blog.leapmotion.com/wp-content/uploads/2017/06/ShaderVariantWarning.jpg
 
# Writing your own shaders {#writing-your-own-shaders}
 
The shaders that are included in the Graphic Renderer module are useful for a wide range of applications, but they will not cover _all_ of your use cases, and don't allow you to take advantage of custom channels, so at some point you might want to try your hand at writing some of your own custom shaders.  This page should give you all the information you need to make sure your shaders are compatible with the graphic renderer.

The Header
----------

Inside of your CGPROGRAM/ENDCG tags, at the top of your code, you will need to define your variants, as well as import the correct cginc file.  When writing a shader, you *must* target a specific type of rendering method.  No shader can both support a baked renderer and a dynamic renderer, so make sure you know the difference and decide which rendering method you would like to target.  You can visit the @ref shader-variants page to see which variants are used for which renderer, or take a look at the [BakedUnlit default shader][] or the [DynamicUnlit default shader][] for examples.  The general structure at the top of your shader will be:

    CGPROGRAM
    #pragma vertex vert
    #pragma fragment frag

    //definitions of your variants
    //include of either the bakedrenderer.cginc or the dynamicrenderer.cginc
    #include "UnityCG.cginc"

Custom channels
---------------

The next area is the area where you define your shader properties.  Things like _MainTex, _Color, or other uniform values.  This is the area where you also define your custom channels.  Custom channels allow you to pipe arbitrary per-graphic information into your shader for you to use as you please.  There are 4 types of custom channels on the Unity side (Float/Vector/Color/Matrix) but on the shader side there are only 3 types due to vectors and colors being of the same type.  You define them like so:

 - `DEFINE_FLOAT_CHANNEL(_ChannelName);` - Define a `float` channel with the name `_ChannelName`.
 - `DEFINE_FLOAT4_CHANNEL(_ChannelName);` - Define a `float4` channel with the name `_ChannelName`.
 - `DEFINE_FLOAT4x4_CHANNEL(_ChannelName);` - Define a `float4x4` channel with the name `_ChannelName`.

You can access custom channels from within the *vertex shader only* using the syntax:

    float perGraphicFloat = getChannel(_ChannelName);
    float4 perGraphicVector = getChannel(_ChannelName);
    float4x4 perGraphicMatrix = getChannel(_ChannelName);

The syntax is the same for all channel types, just make sure that the destination variable matches the channel type!

The vertex shader
-----------------

The vertex shader will look mostly the same, but will include a number of macros.

    v2f_graphic_baked vert (appdata_graphic_baked v) {
      BEGIN_V2F(v);

      v2f_graphic_baked o;
      APPLY_BAKED_GRAPHICS(v,o);

      return o;
    }


The notable elements of the vertex shader include:
 - `appdata_graphic_baked` - The input to the vertex shader must be a specific type compatible with the rendering method you are using.  If you are using the baked rendering method, that is `appdata_graphic_baked` and when using the dynamic renderer that is `appdata_graphic_dynamic`.  
 - `v2f_graphic_baked` - The output of the vertex shader must be a specific type compatible with the rendering method you are using.  If you are using the baked rendering method, that is `v2f_graphic_baked` and when using the dynamic renderer that is `v2f_graphic_dynamic`.  *However* if you want to output custom data you can define a struct of your own, just make sure to include the macro `V2F_GRAPHICAL` at the top, which will make sure all the required fields are included.
 - `BEGIN_V2F` - This macro must always be included at the top of your vertex shader.
 - `APPLY_BAKED_GRAPHICS` - This macro applies all of the transformations of your rendering method.  For the baked renderer the macro is `APPLY_BAKED_GRAPHICS` and for the dynamic renderer it is `APPLY_DYNAMIC_GRAPHICS`.  This macro takes in the appdata struct and fills the output struct with the correct data.

Anywhere inside of the vertex shader (but always *after* BEGIN_V2F) you can use the `getChannel` command to access custom channel data.  You are free to modify the input struct before the `APPLY_GRAPHICS` macro, or modify the output struct after the macro.  Since the macro is what applies the curved space (along with all other features) modifying vertex data before the macro will do the modifications in rectilinear space, and doing them after will do the modifications in curved space.

The Fragment Shader
-------------------

The fragment shader is business as usual!  There is nothing special you _need_ to do in the fragment shader in order to support graphic rendering.  The one thing you _can_ do is do an #ifdef using the `GRAPHICS_HAVE_COLOR` keyword.  If it is defined, the input to the fragment shader should have a color field, which is either taken from vertex colors, a runtime ting, or the multiplication of both!  You can use this keyword to better support colors when writing shaders that include variants.

Differences When Writing A Surface Shader
-----------------------------------------

There are only a few differences you need to be aware of when writing a surface shader.  You can also check out the default [Baked surface shader][] or [Dynamic surface shader][] for examples of how to write surface shaders that support the graphic renderer.

*Always* define both the `GRAPHIC_RENDERER_VERTEX_NORMALS` keyword and the `GRAPHIC_RENDERER_VERTEX_UV_0` keyword along with the definition for your variants.  They are always required for surface shaders.

*Always* define your custom Input struct with the `SURF_INPUT_GRAPHICAL` macro at the top, which will define all of the needed Input data to support the graphic renderer.

*Always* use the surface shader vertex:vert pattern to define a custom vertex shader.  Like the unlit vertex shader, the input must be either of type `appdata_graphic_baked` or `appdata_graphic_dynamic`.  Like the unlit shader, include the `BEGIN_V2F` macro at the top of your vertex shader (but after the `UNITY_INITIALIZE_OUTPUT` macro required in all surface shader vertex shaders).  

*Always* use the `APPLY_BAKED_GRAPHICS_STANDARD`/`APPLY_DYNAMIC_GRAPHICS_STANDARD` macros instead of the non-surface macros used in the unlit vertex shaders.  These macros take in the appdata struct, and your surface shader Input struct.

[BakedUnlit default shader]: https://github.com/leapmotion/UnityModules/blob/master/Assets/LeapMotion/Modules/GraphicRenderer/Shaders/BakedUnlit.shader
[DynamicUnlit default shader]: https://github.com/leapmotion/UnityModules/blob/master/Assets/LeapMotion/Modules/GraphicRenderer/Shaders/DynamicUnlit.shader
[Baked surface shader]: https://github.com/leapmotion/UnityModules/blob/master/Assets/LeapMotion/Modules/GraphicRenderer/Shaders/BakedSurface.shader
[Dynamic surface shader]: https://github.com/leapmotion/UnityModules/blob/master/Assets/LeapMotion/Modules/GraphicRenderer/Shaders/DynamicSurface.shader
 
# Graphic Renderer + Interaction Engine {#gr-plus-ie}

**Q: Can I use the Graphic Renderer with the Interaction Engine? If I curve the graphics of InteractionBehaviours like buttons and sliders, will I still be able to interact with them?**

A: Yes, and yes! Every feature of the Graphic Renderer -- particularly curved spaces -- is compatible with the Interaction Engine. Check out the [Button Builder][buttonbuilder], an example project we put together that demonstrates how the two systems can work in parallel.

# FAQ {#graphic-renderer-faq}

**Q: My objects are not moving/rotating/scaling in play mode, even though I can translate/rotate/scale them in edit mode!**

You are probably using a baked rendering method, with only supports translation at run-time.  If you need rotation or scaling, consider using the dynamic rendering method, or maybe a blend shape feature.

**Q: Some of my objects are not displaying correctly!**

You might be exceeding the maximum graphic count that a group can support, visit the @ref preferences to see what your limit might be, increasing the number might help.

**Q: I am spawning a graphic from a prefab, but the textures are not showing up properly!**

When spawning graphics from a prefab, make sure that your atlas has the `extra textures` property set up to include any textures that you might want to see at runtime.  This ensures that they are included in the atlas so they are available to your instantiated objects at runtime.

**Q: I assigned a texture to my object but I can't see it, even in edit mode!**

Make sure you have pressed the `rebuild atlas` button on the group *that your graphic is attached to*.  Each group has its own atlas, so you will need to make sure that you are rebuilding the correct one!

**Q: I am changing the `vertex color` property on my graphic but I don't see any effect?**

Make sure your rendering method has the `use colors` option enabled, or else vertex colors will not be included in your mesh.

**Q: I changed the layer of my graphic, but it doesn't seem to be having any effect?**

The layer of the graphic is not taken into account by the graphic renderer, since *all graphics for a group are always rendered on the same layer*.  This layer is configured on the *rendering method* and not on the graphic itself.

**Q: My sprites are not showing up correctly?**

Make sure that *all* of your sprites are packed into the same sprite atlas.  They will need to have the same packing tag, and the same settings.  In addition, the current version of unity has a bug involving sprites and atlases, so you might need to enter/exit playmode a few times before they become visible.

# Got a question not answered here?

Head over to [the developer forum][devforum] and post your question! We'll see how we can help.

[devforum]: https://community.leapmotion.com/c/development "Leap Motion Developer Forums"
[buttonbuilder]: https://github.com/leapmotion/Button-Builder