We have an exciting new update to share -- we have created a new set of scripts to help you bind leap motion data to hands.

You continue to have the ability to auto-rig a wide array of FBX hand assets with one or two button presses. You can quickly iterate between a modelling package and seeing the models driven by live hand motion in Unity. A powerful benefit.

In this post, we'll provide a detailed overview on how to use our new Hand Binder pipeline, as well as some explanation of what happens under the hood. At the end, we will take a step back with some best practices for both building hand assets from scratch or choosing hand assets from a 3D asset store.

<https://github.com/leapmotion/UnityModules/tree/feat-HandBinder>

|

- Drag and drop GameObjects from the scene and connect them to leap data with ease.

- Auto-rig function to automatically search, assign and calculate rotation offsets for you.

- Manually add offsets to any finger bone.

- Debugging options to help you understand the leap data.

 |

* * * * *

**Hand Binder Pipeline**
========================

The new Hand Binder Monobehavior script is the connection between leap motion data and the game objects you want to attach to finger bone.

Note: The hand binder is a complete overhaul of the previous Hand Rigging solution and is intended to replace it.

#### Video Guide

[2021-01-06 10-45-55.mp4](https://ultrahaptics.atlassian.net/wiki/download/attachments/2530541697/2021-01-06%2010-45-55.mp4?version=1&modificationDate=1609930249413&cacheVersion=1&api=v2)

* * * * *

#### **Quick Start:**

We have included a step-by-step editor window to the Hand Module. Find it in the top menu under Ultraleap/Hand Rigging Documentation. This is a step by step guide which will help you through the process right in the editor. A quick preview of the window is available below.

[1.mp4](https://ultrahaptics.atlassian.net/wiki/download/attachments/2530541697/1.mp4?version=1&modificationDate=1609850246632&cacheVersion=1&api=v2&width=340)

* * * * *

#### **Understanding the Hand Binder Inspector**

![](https://ultrahaptics.atlassian.net/wiki/download/attachments/2530541697/image-20210105-134058.png?version=1&modificationDate=1609854062337&cacheVersion=1&api=v2 "Creative Team > 2021/01/05 > Hands Module > image-20210105-134058.png")[Drag and Drop.mp4](https://ultrahaptics.atlassian.net/wiki/download/attachments/2530541697/Drag%20and%20Drop.mp4?version=1&modificationDate=1609854569276&cacheVersion=1&api=v2&width=340)

#### **Assigning Bones Manually:**

If the hand model you have doesn't have a clear naming convention, you can still use the **Hand Binder** to connect leap data to the hand model. Simply drag and drop the GameObject in the scene to a slot on the hand graphic.

**Note**: You **DO NOT **have to assign every bone, the hand binder script is able to push data to one or all the bones of the hand.

[Rigging Options.mp4](https://ultrahaptics.atlassian.net/wiki/download/attachments/2530541697/Rigging%20Options.mp4?version=1&modificationDate=1609851813696&cacheVersion=1&api=v2&width=340)

#### *Rigging Options*

**Handedness** -- which hand will this target? Changing this will provide either the left or right hand data.

**Use Metacarpal bones** -- does this hand have joints for metacarpal bones? If so, would you like to use them?

**Set the positions of the fingers **-- should each assigned finger position be set to match the position of the leap data?

**Custom Bone Definitions** -- a scriptable object you can create to define specific bone names.

![](https://ultrahaptics.atlassian.net/wiki/download/thumbnails/2530541697/image-20210105-134830.png?version=1&modificationDate=1609854513844&cacheVersion=1&api=v2&width=340 "Creative Team > 2021/01/05 > Hands Module > image-20210105-134830.png")

If you wish to extend the names of the bones that can be found, right click in the project window and select Create -- Ultraleap --HandBinderBoneDefinitions.

You have now created a scriptable object that can be assigned to the Hand Binder Script under the Rigging Options Button. You are free to change the names in the scriptable object to match the definition of rig you have.

#### *Arm Rigging*

**Elbow Transform - **The elbow transform of the model

**Elbow Position Offset - **The offset applied to elbows position

**Elbow Rotation Offset - **The offset applied to the elbows rotation

**Elbow Length - **The length of the elbow to maintain the correct offset from the wrist.

#### *Debug Options*

[Debug Options.mp4](https://ultrahaptics.atlassian.net/wiki/download/attachments/2530541697/Debug%20Options.mp4?version=1&modificationDate=1609851922433&cacheVersion=1&api=v2&width=340)

**Debug Leap Hand **-- shows a gizmo for each joint of the leap hand. In editor this will be set to the edit time pose set in the leap service provider.

**Debug Leap Rotation Axis **-- shows the rotation axis for each joint of the leap hand.

**Debug Model Transforms **-- shows a gizmo for each joint that has been assigned into the leap hand binder.

**Debug Model Rotation Axis **-- shows the rotation axis for each joint that is assigned.

**Gizmo Size** -- the size of the gizmos in the scene.

**Reset Hand/Align with leap pose- **do you want the assigned hand to be set to the default leap pose during edit mode? This is a useful option that can reset the bones back to their default pose.

#### *Fine Tuning Options*

[Fine Tuning 1.mp4](https://ultrahaptics.atlassian.net/wiki/download/attachments/2530541697/Fine%20Tuning%201.mp4?version=2&modificationDate=1609853072624&cacheVersion=1&api=v2&width=340)

**Global Finger Rotation Offset** -- the rotation offset that is applied to rotate the model's fingers to the rotation of the leap finger data.

**Wrist Rotation Offset** -- the rotation offset that is applied to rotate the models wrist to the rotation of the leap wrist data.

**Recalculate offsets** -- click this to have the script automatically calculate the rotation difference for you. This may require a couple of clicks for the rotation to be calculated and slight user adjustment to correctly calculate this.

**Add Finger Offset** -- ever wondered what your hand would look like with extra long fingers? Well now you can! Simply add a new finger offset, change the finger type and bone type to the bone you wish to adjust. Then fiddle with the position and rotation values until you get the desired effect.