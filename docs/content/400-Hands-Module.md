# Hands Modules {#hands-module}

We have an exciting new update to share – we have created a new set of scripts to help you bind leap motion data to hands.

You continue to have the ability to auto-rig a wide array of FBX hand assets with one or two button presses. You can quickly iterate between a modelling package and seeing the models driven by live hand motion in Unity. A powerful benefit.

In this post, we’ll provide a detailed overview on how to use our new Hand Binder pipeline, as well as some explanation of what happens under the hood. At the end, we will take a step back with some best practices for both building hand assets from scratch or choosing hand assets from a 3D asset store.

- Drag and drop GameObjects from the scene and connect them to leap data with ease.

- Auto-rig function to automatically search, assign and calculate rotation offsets for you.

- Manually add offsets to any finger bone.

- Debugging options to help you understand the leap data.

# Hand Binder {#hand-binder}

The new Hand Binder Monobehavior script is the connection between leap motion data and the game objects you want to attach to finger bone.

Note: The hand binder is a complete overhaul of the previous Hand Rigging solution and is intended to replace it.

# Video Guide {#video-guide}

\htmlonly
<video class="ie-example-video" src="HandsModule_Guide.webm" autoplay loop></video>
\endhtmlonly

2021-03-11 10-14-45.mp4

# Assigning Bones Manually:
If the hand model you have doesn't have a clear naming convention, you can still use the Hand Binder to connect leap data to the hand model. Simply drag and drop the GameObject you wish to bind from the scene into the slots in the Bind Hand pop up window.

Note: You DO NOT have to assign every bone, the hand binder script is able to push data to one or all the bones of the hand.

\htmlonly
<video class="ie-example-video" src="HandsModule_HandBinder.webm" autoplay loop></video>
\endhtmlonly

# Understanding the Hand Binder Inspector

![](@ref images/HandsModule_Inspector.png)

**Hand Type** – which hand will this target? Changing this will provide either the left or right hand data.

**Use Metacarpal bones** – does this hand have joints for metacarpal bones? If so, would you like to use them?

**Set bone positions** – should each assigned Gameobjects position be set to match the position of the leap data?

**Show Debug Options**
**Debug Leap Hand** – shows a gizmo for each joint of the leap hand. In editor this will be set to the edit time pose set in the leap service provider.

**Debug Leap Rotation Axis** – shows the rotation axis for each joint of the leap hand.

**Debug Model Transforms** – shows a gizmo for each joint that has been assigned into the leap hand binder.

**Debug Model Rotation Axis** – shows the rotation axis for each joint that is assigned.

**Gizmo Size** – the size of the gizmos in the scene.

**Reset Hand/Align with leap pose** - do you want the assigned hand to be set to the default leap pose during edit mode? This is a useful option that can reset the bones back to their default pose.

**Fine Tuning Options**
Global Finger Rotation Offset – the rotation offset that is applied to rotate the model's fingers to the rotation of the leap finger data.

**Wrist Rotation Offset** – the rotation offset that is applied to rotate the models wrist to the rotation of the leap wrist data.

**Recalculate offsets** – click this to have the script automatically calculate the rotation difference for you. This may require a couple of clicks for the rotation to be calculated and slight user adjustment to correctly calculate this.

**Add Finger Offset** – ever wondered what your hand would look like with extra long fingers? Well now you can! Simply add a new finger offset, change the finger type and bone type to the bone you wish to adjust. Then fiddle with the position and rotation values until you get the desired effect.

 

 