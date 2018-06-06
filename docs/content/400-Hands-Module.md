# Hands Module {#hands-module}

_**Editor's Note:** This article is re-printed from a [blog post][hands-module-blog-post] documenting the Hands Module shortly after its release. It is still up-to-date, as the Hands Module's workflow has not changed significantly since its publication._

[hands-module-blog-post]: http://blog.leapmotion.com/hands-module-2-0-bring-your-hand-designs-to-life-in-two-minutes-or-less/ 

Creating new 3D hand assets for your Leap Motion projects can be a real challenge. That’s why, based on your feedback, we’ve massively automated and streamlined the pipeline for connecting 3D models to our Core Assets with Hands Module 2.0 – so what used to take hours only takes a minute or two. You can get the new module and updated assets on [our developer portal](https://developer.leapmotion.com/unity).

You now have the ability to auto-rig a wide array of FBX hand assets with one or two button presses. This has the powerful benefit of being able to quickly iterate between a modeling package and seeing the models driven by live hand motion in Unity. Even if you’re a veteran modeler-rigger-animator, it’s a singular experience to bring hand models that you’ve been sculpting and rigging into VR and see them come to life with your own hand motions.

In this post, we’ll provide a detailed overview on how to use our new autorig pipeline, as well as some explanation of what happens under the hood. At the end, we’ll take a step back with some best practices for both building hand assets from scratch or choosing hand assets from a 3D asset store.

# Autorigging with LeapHandsAutorig {#hands-auto-rigging-tool}

The new LeapHandAutorig Monobehavior script acts as a quarterback for an array of scripts and methods which comprise the rigged hands setup process. Sitting at the top of a hands hierarchy, the script runs in the editor. The Autorig button in the Inspector sets off a chain of actions that works through the steps outlined in the chart below.

![](http://blog.leapmotion.com/wp-content/uploads/2016/07/hands-module-4.png)

After autorigging, the LeapHands Autorig Inspector console acts as a central control panel to push values to the other Leap Motion rigging scripts. This allows you to test the model quickly and set certain values centrally, instead of digging through the hierarchy to set values in all the scripts manually.

Autorigging can act on a variety of FBX assets, and works in two different ways, depending on whether the asset has a Unity Mecanim Humanoid definition. If so, then LeapHandsAutorig finds and assign joints based on this mapping. If not, then LeapHandsAutorig searches the hierarchy below its Transform by a list of names typical of common character rigging practices.

![](http://blog.leapmotion.com/wp-content/uploads/2016/07/hands-module-6.png)

# Step 1: Setting the Scene {#hands-auto-rig-step-1}

To try the autorigging for yourself, the Hand Module download includes an example file, Assets/LeapMotionModules/Hands/Examples/Rigged_Hands_AutoRig_Example.unity. This contains a Leap VR camera rig: LMHeadMountedRig. (For an explanation of how the camera rig works, [our posts on the new Unity Core Assets.](http://blog.leapmotion.com/redesigning-unity-core-assets-part-ii-new-features-4-1-0/).)

There are two sets of FBX hands in this file for testing the autorigging. They’ve simply been dragged from the /Hands/Models/ folder and parented under the VR camera rig. One set of hands is made of two FBXs and illustrates how the autorigger can start its process by finding names.  The other is a single file with a simple human body joint hierarchy to illustrate autorigging with Unity Mecanim’s joint mapping as a starting point.

# Step 2A: Separate FBXs {#hands-auto-rig-step-2A}

[Bite-Size: How to AutoRig FBX Hand Assets in Your VR Project](https://youtu.be/suOhoiX5RMI)

In this example, the first set of hands (under the GraphicsModels transform) is comprised of a separate FBX for each hand. Since these don’t have a Mecanim Humanoid mapping associated with them, LeapHandsAutorig will use their hierarchy’s naming to set them up.

+Drag the LeapHandsAutorig script found in the /Hands/Scripts/ folder to the GraphicsModels transform. These hand models happen to be built with metacarpal transforms at the base of each finger. So check the “Use Metacarpal’s” checkbox in the Inspector. This will take those extra joints into account when assigning RiggedFinger scripts. Then press the Autorig button at the bottom of the Inspector and the hands are ready to play!

There are several ways to verify whether various parts of the autorigging were successful. Start by seeing if the previously empty fields in the LeapHandsAutorig component now have references in them. Then, to verify that the hands are added to the HandPool and ready to be driven, select the LMHeadMountedRig/CenterEyeAnchor/LeapSpace/LeapHandController transform and check for the GraphicsHands ModelGroup in the HandPool component.

You can also verify that the hands have been setup correctly by checking the Set Editor Leap Pose checkbox in the LeapHandsAutorig’s Inspector. This will pose the hands in the Leap editor pose. When the autorigging is run, snapshots of the hands’ hierarchies are stored. Then, if you uncheck that checkbox, the model is returned to this stored pose.

# Step 2B: Single FBX With Mecanim Humanoid Hierarchy {#hands-auto-rig-step-2B}

[Bite-Size: How to AutoRig FBX Hand Assets (with Mancanim Humaniod) in Your VR Project](https://www.youtube.com/watch?v=ZvMXiLftWqc)

In this alternative example, the LoPoly_Rigged_Hands_Skeleton transform is an FBX with a simple but complete body joint hierarchy and a + Mecanim Humanoid definition in its Unity Avatar.

Drag the LeapHandsAutorig script to this transform and click the Autorig button. In this case, if you check the SetEditorLeapPose checkbox, you’ll see that the hands’ palms are flipped. So for this model, you can check the FlipPalms checkbox. This reverses the direction of the ModelPalmFacing vectors for each RiggedHand script and all of the RiggedFinger scripts as well.

# Step 3: RiggedHand and RiggedFingers Are Set Up Automatically #{hands-auto-rig-step-3}

One of the main tasks of the LeapHandAutorig component is to find hand transforms and assign RiggedHand components, then to find the base transform for each finger and assign RiggedFinger components. After autorigging, you can find them quickly by clicking on their references in the LeapHandAutorig’s Inspector. This expands the hierarchy and highlights their individual transforms for easy selection. These are the script components that receive and translate tracking data from Leap Motion’s Core Assets scripts and actually drive the rigged hand models at runtime.

The RiggedHand script contains references to the palm and forearm (if they exist) as well as reference to the five RiggedFinger components in its hierarchy. The ModelPalmFacing and ModelFingerPointing vectors represent the cardinal direction the the palm and fingers face. These, and the several remaining fields, are identical to those exposed in the LeapHandAutorig script. When those values are changed, those values are pushed here.

![](http://blog.leapmotion.com/wp-content/uploads/2016/07/hands-module-2.png)

Each RiggedFinger script contains references for its three child bone transforms and one of five finger types. They also have fields for the cardinal-direction-facing vectors for the palm and the direction of its children bones.  Again, like those in the RiggedHand script, these vectors are calculated by methods within the RiggedFingers script, but can be changed via the central interface of the LeapHandsAutorig.

Each RiggedFinger component also has a significant checkbox called Deform Position. This causes the joint transforms to not only be rotated by Leap Motion tracking but to be positioned as well. To take advantage of this feature, the FBX model needs to have been built with joints close to human proportions and weighted well enough to allow joints to move without polygon tearing. This field then allows for scaling and proportioning the rigged model to the user’s tracked hand.

![](http://blog.leapmotion.com/wp-content/uploads/2016/07/hands-module-7.png)

# Making New Hand Models or Choosing Hand Assets #{hands-new-hand-models}

Now that we’ve seen what Hands Module 2.0 can do, it’s time to find the right assets for your project! Before building and rigging new hand models in a 3D modeling package to use with the Hands Module, we recommend that you be fairly experienced with hand anatomy, 3D polygon modeling and edgeloop topology, joint orientations, and weighting.

That being said, the steps outlined below are equally relevant if you’re choosing (and possibly modifying) assets from a 3D asset store such as the Unity Asset Store or Turbosquid. In the end, all that’s need is a well-modeled, jointed, named and weighted mesh, nothing out of the ordinary for a typical game character rig. That said, for quality results, it’s important to address the following details thoroughly.

Sculpting and topology:  Sculpting something that can bend and deform well is more that simply creating a visually appealing shape. You’ll want to think about and plan how your model will look when it’s stretched to its limits, curled into a fist or other extreme poses. We strongly recommend topology that features edgeloops flowing along the creases of the hand, rather than a uniform distribution of polygons. This is critical for good deformations.

![](http://blog.leapmotion.com/wp-content/uploads/2016/07/hands-module-3.png)

Performance: Since you’re probably creating these hands for a VR application, it’s good to remember that these hands get rendered twice. To keep your framerates high, polygon budgets and draw calls should be managed. (Underscore that several times if you’re creating a mobile application.)

Joint and File Naming: To allow the autorig script find-by-name approach, joints’ names should contain one of the possible strings for each joint type according to the list below. These are pretty standard naming conventions for 3D rigs, and the common 3D packages have tools for renaming hierarchies quickly. If you plan to use Mecanim’s full Humanoid mapping, this naming is not critical.

* Right hand – “right” or “_r”
* Left hand – “left” or “_l”
* Palm – “palm”
* Thumb – “thumb” or “tmb”
* Index finger – “index” or “idx”
* Middle finger – “middle” or “mid”
* Ring finger – “ring”
* Pinky – “pinky” or “pin”

Joint Orientation: Having proper joint orientations is critical for a couple of reasons – most importantly, for the RiggedHand and RiggedFinger scripts to be able to rotate joints at the correct angle. Joints need to be oriented with one axis pointed directly down the joint, towards its child, and another axis pointed along its main rotation axis. Notably, while this is common practice for character riggers, not all assets on the asset store are built this way. This is probably the first thing to examine when determining if an asset will work with our RiggedHand and RiggedFinger scripts.

Keep in mind that the end user’s hands will be curling anatomically. Understanding the finer details – like how fingers curl toward the center of the palm, rather just folding straight in – will streamline your development and help you get more convincing poses out of your rigged hands.

![](http://blog.leapmotion.com/wp-content/uploads/2016/07/hands-module-1.png)

Vertex Weighting for Range of Motion and Good Deformation: Since your rigged hands may be driven by many different end users, hand models for Leap Motion tracking need to deform well through a rich range of motions. Joint placement and careful weighting for good deformations is important for quality posing.

Beware the Uncanny Valley: Hyper-realism isn’t always the best approach in VR. Users almost always respond better to stylized or cartoony hands.

When making 3D models for animation in the past, we’ve often used the workflow of throwing in joints and weights and a few rough poses early in the modeling process. That way, we can see how the model deforms while iterating the sculpture.

But now going all the way from your 3D package to seeing your hand models in VR – driven by your hands – can take just a few moments! Iterating models and quickly seeing how they perform during live tracking is a very new and interesting workflow.

