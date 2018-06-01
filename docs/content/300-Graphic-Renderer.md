# Graphic Renderer {#graphic-renderer}

foo

# Graphic Renderer + Interaction Engine {#gr-plus-ie}

**Q: Can I use the Graphic Renderer with the Interaction Engine? If I curve the graphics of InteractionBehaviours like buttons and sliders, will I still be able to interact with them?**

A: Yes, and yes! Every feature of the Graphic Renderer -- particularly curved spaces -- is compatible with the Interaction Engine. Check out the [Button Builder][buttonbuilder], an example project we put together that demonstrates how the two systems can work in parallel.

# FAQ {#graphic-renderer-faq}

**Q: My objects are not moving/rotating/scaling in play mode, even though I can translate/rotate/scale them in edit mode!**

You are probably using a baked rendering method, with only supports translation at run-time.  If you need rotation or scaling, consider using the dynamic rendering method, or maybe a blend shape feature.

**Q: Some of my objects are not displaying correctly!**

You might be exceeding the maximum graphic count that a group can support, visit the [preferences menu][] to see what your limit might be, increasing the number might help.

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
[preferences menu]: https://github.com/leapmotion/UnityModules/wiki/Preferences-menu
[buttonbuilder]: https://github.com/leapmotion/Button-Builder