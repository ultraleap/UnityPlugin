# Hands Module

We have an exciting new update to share regarding the Hands module, a new set of scripts have been developed to help you bind leap motion data to your own hands assets.

You continue to have the ability to auto-rig a wide array of FBX hand assets with one or two button presses, this has the powerful benefit of being able to quickly iterate between a modelling package and seeing the models driven by live hand motion in Unity.

In this post, we’ll provide a detailed overview on how to use our new  **Hand Binder**  pipeline, as well as some explanation of what happens under the hood. At the end, we will take a step back with some best practices for both building hand assets from scratch or choosing hand assets from a 3D asset store.

[https://github.com/leapmotion/UnityModules/tree/feat-HandBinder](https://github.com/leapmotion/UnityModules/tree/feat-HandBinder)

-   Easily drag and drop  _GameObjects_ from the scene and connect them to leap data
    
-   Autorig function to automatically search, assign and calculate rotation offsets for you.
    
-   Manually add offsets to any finger bone
    
-   Debugging options to help you understand the leap data
    

----------

# **Hand Binder Pipeline**

The new Hand Binder Monobehavior script is the connection between leap motion data and the game objects you want to attach to finger bone.

_Note: The hand binder is a complete overhaul of the previous Hand Rigging solution and is intended to replace it._

#### Video Guide

[2021-01-06 10-45-55.mp4](https://ultrahaptics.atlassian.net/wiki/download/attachments/2530541697/2021-01-06%2010-45-55.mp4?version=1&modificationDate=1609930249413&cacheVersion=1&api=v2)

----------

#### **Quick Start:**

We have included a step-by-step editor window to the Hand Module which you can find at the top menu under  _Ultraleap/Hand Rigging Documentation._ This is a step by step guide which will help you through the process straight in the editor, a quick preview of the window is available below.

[1.mp4](https://ultrahaptics.atlassian.net/wiki/download/attachments/2530541697/1.mp4?version=1&modificationDate=1609850246632&cacheVersion=1&api=v2&width=340)

----------

#### **Understanding the Hand Binder Inspector**

![](https://ultrahaptics.atlassian.net/wiki/download/attachments/2530541697/image-20210105-134058.png?version=1&modificationDate=1609854062337&cacheVersion=1&api=v2 "Creative Team > 2021/01/05 > Hands Module > image-20210105-134058.png")

[Drag and Drop.mp4](https://ultrahaptics.atlassian.net/wiki/download/attachments/2530541697/Drag%20and%20Drop.mp4?version=1&modificationDate=1609854569276&cacheVersion=1&api=v2&width=340)

#### **Assigning Bones Manually:**

If the hand model you have does not have a clear naming convention, you are still able to use the  **Hand Binder**  to connect leap data to the hand model. Simply drag and drop the GameObject in the scene to a slot on the hand graphic.

**Note:**  You  **DO NOT** have to assign every bone, the hand binder script is able to push data to just one or all the bones of the hand.

[Rigging Options.mp4](https://ultrahaptics.atlassian.net/wiki/download/attachments/2530541697/Rigging%20Options.mp4?version=1&modificationDate=1609851813696&cacheVersion=1&api=v2&width=340)

#### _Rigging Options_

Handedness - Which hand will this target, changing this will provide either the left or right hand data.

Use Metacarpal bones - Does this hand have joints for metacarpal bones, if so would you like to use them?

Set the positions of the fingers - Should each assigned fingers position be set to match the position of the leap data.

Custom Bone Definitions - A scriptable object you can create to define specific bone names.

![](https://ultrahaptics.atlassian.net/wiki/download/thumbnails/2530541697/image-20210105-134830.png?version=1&modificationDate=1609854513844&cacheVersion=1&api=v2&width=340 "Creative Team > 2021/01/05 > Hands Module > image-20210105-134830.png")

If you wish to extend the names of the bones that can be found, simply right click in the project window, select  _Create/Ultraleap/HandBinderBoneDefinitions._

You have now created a scriptable object that can be assigned to the Hand Binder Script under the  _Rigging Options Button._ You are free to change the names in the scriptable object to match the definition of rig you have.

#### _Debug Options_

[Debug Options.mp4](https://ultrahaptics.atlassian.net/wiki/download/attachments/2530541697/Debug%20Options.mp4?version=1&modificationDate=1609851922433&cacheVersion=1&api=v2&width=340)

Debug Leap Hand - Show a gizmo for each joint of the leap hand, in editor this will be set to the edit time pose set in the leap service provider.

Debug Model Transforms - Show a gizmo for each joint that has been assigned into the leap hand binder.

Gizmo Size - The size of the gizmos in the scene.

Set Editor Pose - Do you want the assigned hand to be set to the leap hand during edit mode. This is a useful option to press if you get stuck to reset the bones back to there default pose.

#### _Fine Tuning Options_

[Fine Tuning 1.mp4](https://ultrahaptics.atlassian.net/wiki/download/attachments/2530541697/Fine%20Tuning%201.mp4?version=2&modificationDate=1609853072624&cacheVersion=1&api=v2&width=340)

Global Finger Rotation Offset - The rotation offset that is applied to rotate the models fingers to the rotation of the leap finger data.

Wrist Rotation Offset - The rotation offset that is applied to rotate the models wrist to the rotation of the leap wrist data.

Recalculate offsets - Click this to have the script automatically calculate the rotation difference for you, this may require a couple of clicks for the rotation to be calculated and slight user adjustment to correctly calculate this.

Add Finger Offset - Ever wondered what your hand would look like with extra long fingers? Well now you can! Simply add a new finger offset, change the finger type and bone type to the bone you wish to adjust. Then fiddle with the position and rotation values until you get the desired effect.

[Fine Tuning 1.mp4](https://ultrahaptics.atlassian.net/wiki/download/attachments/2530541697/Fine%20Tuning%201.mp4?version=2&modificationDate=1609853072624&cacheVersion=1&api=v2)

----------

# **Making Hand Models or Choosing Hand Assets**

Now that we’ve seen what the new  **Hand Binder**  scripts can do, it’s time to find the right assets for your project! Before building and rigging new hand models in a 3D modeling package to use with the Hands Module, we recommend that you be fairly experienced with 3D modelling as you may need to adjust topology to get the correct deformation of models.

That being said, the steps outlined below are equally relevant if you’re choosing (and possibly modifying) assets from a 3D asset store such as the Unity Asset Store. In the end, all that’s needed is a well-modelled, jointed, named and weighted mesh, nothing out of the ordinary for a typical game character rig. That said, for quality results, it’s important to address the following details.

#### **Sculpting and topology**:

Sculpting something that can bend and deform well is more than simply creating a visually appealing shape. You’ll want to think about and plan how your model will look when it’s stretched to its limits, curled into a fist or other extreme poses. We strongly recommend topology that features edgeloops flowing along the creases of the hand, rather than a uniform distribution of polygons. (This is critical for good deformations).

#### **Performance:**

Since you’re probably creating these hands for a VR application, it’s good to remember that these hands get rendered twice. To keep your framerates high, polygon budgets and draw calls should be managed. (Underscore that several times if you’re creating a mobile application.)

#### **Joint and File Naming:**

To allow the  **Hand Binders Auto Rig**  function to find the correct bones of the hand, you should ensure the joints of the hand use names from the list below. These are standard naming conventions and common in 3D packages, modelling packages will have tools for renaming hierarchies quickly if needed.

-   thumb
-   index
-   middle
-   ring
-   pinky/little
-   wrist/hand/palm
-   elbow/upperArm

**Joint Orientation:**

[Joint orientation.mp4](https://ultrahaptics.atlassian.net/wiki/download/attachments/2530541697/Joint%20orientation.mp4?version=1&modificationDate=1609854652666&cacheVersion=1&api=v2&width=340)

Having proper joint orientations is no longer crucial but still highly advised. You are now able to add a global rotation offset to both the wrist and the fingers separately.

Joints need to be oriented with one axis pointed directly down the joint, towards its child, and another axis pointed along its main rotation axis. Notably, while this is common practice for character riggers, not all assets on the asset store are built this way. Keep in mind that the end user’s hands will be curling anatomically. Understanding the finer details – like how fingers curl toward the center of the palm, rather just folding straight in – will streamline your development and help you get more convincing poses out of your rigged hands.

We have included a  **Default Leap Hand**  as an FBX included in the hand module. You can find this at  _Assets/Plugins/LeapMotion/Modules/Hands/Models_.

This hand is posed in the default leap pose and has all the joints rotated in the same direction as leap generates, this is a good basis for you to use as the skeletal structure in your preferred modelling program and to either model around it or attach it to your hand.

#### **Vertex Weighting for Range of Motion and Good Deformation:**

Since your rigged hands may be driven by many different end users, hand models for Leap Motion tracking need to deform well through a rich range of motions. Joint placement and careful weighting for good deformations is important for quality posing.

When making 3D models for animation in the past, we’ve often used the workflow of throwing in joints and weights and a few rough poses early in the modeling process. That way, we can see how the model deforms while iterating the sculpture.

But now going all the way from your 3D package to seeing your hand models in VR – driven by your hands – can take just a few moments! Iterating models and quickly seeing how they perform during live tracking is a very new and interesting workflow.
