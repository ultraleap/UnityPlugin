using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Leap.Unity {
  /**LeapHandAutoRig automates setting up the scripts that drive 3D skinned mesh hands. */
  [AddComponentMenu("Leap/Auto Rig Hands")]
  public class LeapHandsAutoRig : MonoBehaviour {
    private HandPool HandPoolToPopulate;
    public Animator AnimatorForMapping;
    private RuntimeAnimatorController runtimeAnimatorController;
    public string ModelGroupName = null;
    [Tooltip("Set to True if each finger has an extra trasform between palm and base of the finger.")] 
    public bool ModelHasMetaCarpals;
    [Header("RiggedHand Components")]
    public RiggedHand RiggedHand_L;
    public RiggedHand RiggedHand_R;
    [Header("HandTransitionBehavior Components")]
    public HandTransitionBehavior HandTransitionBehavior_L;
    public HandTransitionBehavior HandTransitionBehavior_R;
    [Tooltip("Test")]
    [Header("RiggedFinger Components")]
    public RiggedFinger RiggedFinger_L_Thumb;
    public RiggedFinger RiggedFinger_L_Index;
    public RiggedFinger RiggedFinger_L_Mid;
    public RiggedFinger RiggedFinger_L_Ring;
    public RiggedFinger RiggedFinger_L_Pinky;
    public RiggedFinger RiggedFinger_R_Thumb;
    public RiggedFinger RiggedFinger_R_Index;
    public RiggedFinger RiggedFinger_R_Mid;
    public RiggedFinger RiggedFinger_R_Ring;
    public RiggedFinger RiggedFinger_R_Pinky;
    [Header("Palm & Finger Direction Vectors.")]
    public Vector3 modelFingerPointing_L = new Vector3(0, 0, 0);
    public Vector3 modelPalmFacing_L = new Vector3(0, 0, 0);
    public Vector3 modelFingerPointing_R = new Vector3(0, 0, 0);
    public Vector3 modelPalmFacing_R = new Vector3(0, 0, 0);
    [Tooltip("Toggling this value will reverse the ModelPalmFacing vectors to both RiggedHand's and all RiggedFingers.  Change if hands appear backward when tracking.")]
    [SerializeField]
    public bool FlipPalms = false;
    [SerializeField]
    [HideInInspector]
    private bool flippedPalmsState = false;
    private IKMarkersAssembly ikMarkersAssemblyPrefab;
    private IKMarkersAssembly iKMarkersAssembly;

    /**AutoRig() Calls AutoRigMecanim() if a Unity Avatar exists.  Otherwise, AutoRigByName() is called.  
     * Then it immediately RiggedHand.StoreJointStartPose() to store the rigged asset's original state.*/
    [ContextMenu("AutoRigHands")]
    public void AutoRigHands() {
      HandPoolToPopulate = GameObject.FindObjectOfType<HandPool>();
      AnimatorForMapping = gameObject.GetComponent<Animator>();
      if (AnimatorForMapping != null) {
        if (AnimatorForMapping.isHuman == true) {
          AutoRigMecanim();
          HandTransitionBehavior_L.OnSetup();
          HandTransitionBehavior_R.OnSetup();
          RiggedHand_L.StoreJointsStartPose();
          RiggedHand_R.StoreJointsStartPose();
          return;
        }
        else {
          Debug.LogWarning("The Mecanim Avatar for this asset does not contain a valid IsHuman definition.  Attempting to auto map by name.");
        }
      }
      AutoRigByName();
    }
    /**Allows a start pose for the rigged hands to be created and stored anytime. */
    [ContextMenu("StoreStartPose")]
    public void StoreStartPose() {
      if (RiggedHand_L && RiggedHand_R) {
        RiggedHand_L.StoreJointsStartPose();
        RiggedHand_R.StoreJointsStartPose();
      }
      else Debug.LogWarning("Please AutoRig before attempting to Store Start Pose");
    }
    /**Uses transform names to discover and assign RiggedHands scripts,
     * then calls methods in the RiggedHands that use transform nanes to discover fingers.*/
    [ContextMenu("AutoRigByName")]
    void AutoRigByName() {
      List<string> LeftHandStrings = new List<string> { "left" };
      List<string> RightHandStrings = new List<string> { "right" };

      //Assigning these here since this component gets added and used at editor time
      //HandPoolToPopulate = GameObject.FindObjectOfType<HandPool>();
      Reset();

      //Find hands and assigns RiggedHands
      Transform Hand_L = null;
      foreach (Transform t in transform) {
        if (LeftHandStrings.Any(w => t.name.ToLower().Contains(w))) {
          Hand_L = t;
        }
      }
      if (Hand_L != null) {
        RiggedHand_L = Hand_L.gameObject.AddComponent<RiggedHand>();
        HandTransitionBehavior_L = Hand_L.gameObject.AddComponent<HandEnableDisable>();
        RiggedHand_L.Handedness = Chirality.Left;
        RiggedHand_L.SetEditorLeapPose = false;
        RiggedHand_L.UseMetaCarpalsOnSetup = ModelHasMetaCarpals;
        RiggedHand_L.SetupRiggedHand();

        RiggedFinger_L_Thumb = (RiggedFinger)RiggedHand_L.fingers[0];
        RiggedFinger_L_Index = (RiggedFinger)RiggedHand_L.fingers[1];
        RiggedFinger_L_Mid = (RiggedFinger)RiggedHand_L.fingers[2];
        RiggedFinger_L_Ring = (RiggedFinger)RiggedHand_L.fingers[3];
        RiggedFinger_L_Pinky = (RiggedFinger)RiggedHand_L.fingers[4];

        modelFingerPointing_L = RiggedHand_L.modelFingerPointing;
        modelPalmFacing_L = RiggedHand_L.modelPalmFacing;

        RiggedHand_L.StoreJointsStartPose();
      }
      Transform Hand_R = null;
      foreach (Transform t in transform) {
        if (RightHandStrings.Any(w => t.name.ToLower().Contains(w))) {
          Hand_R = t;
        }
      }
      if (Hand_R != null) {
        RiggedHand_R = Hand_R.gameObject.AddComponent<RiggedHand>();
        HandTransitionBehavior_R = Hand_R.gameObject.AddComponent<HandEnableDisable>();
        RiggedHand_R.Handedness = Chirality.Right;
        RiggedHand_R.SetEditorLeapPose = false;
        RiggedHand_R.UseMetaCarpalsOnSetup = ModelHasMetaCarpals;
        RiggedHand_R.SetupRiggedHand();

        RiggedFinger_R_Thumb = (RiggedFinger)RiggedHand_R.fingers[0];
        RiggedFinger_R_Index = (RiggedFinger)RiggedHand_R.fingers[1];
        RiggedFinger_R_Mid = (RiggedFinger)RiggedHand_R.fingers[2];
        RiggedFinger_R_Ring = (RiggedFinger)RiggedHand_R.fingers[3];
        RiggedFinger_R_Pinky = (RiggedFinger)RiggedHand_R.fingers[4];

        modelFingerPointing_R = RiggedHand_R.modelFingerPointing;
        modelPalmFacing_R = RiggedHand_R.modelPalmFacing;

        RiggedHand_R.StoreJointsStartPose();
      }
      //Find palms and assign to RiggedHands
      //RiggedHand_L.palm = AnimatorForMapping.GetBoneTransform(HumanBodyBones.LeftHand);
      //RiggedHand_R.palm = AnimatorForMapping.GetBoneTransform(HumanBodyBones.RightHand);

      if (ModelGroupName == "" || ModelGroupName != null) {
        ModelGroupName = transform.name;
      }
      HandPoolToPopulate.AddNewGroup(ModelGroupName, RiggedHand_L, RiggedHand_R);
    }

    /**Uses Mecanim transform mapping to find hands and assign RiggedHands scripts 
     * and to find base of each finger and asisng RiggedFinger script.
     * Then calls methods in the RiggedHands that use transform names to discover fingers */
    [ContextMenu("AutoRigMecanim")]
    void AutoRigMecanim() {
      //Assigning these here since this component gets added and used at editor time
      //AnimatorForMapping = gameObject.GetComponent<Animator>();
      //HandPoolToPopulate = GameObject.FindObjectOfType<HandPool>();
      Reset();


      //Find hands and assign RiggedHands
      Transform Hand_L = AnimatorForMapping.GetBoneTransform(HumanBodyBones.LeftHand);
      if (Hand_L.GetComponent<RiggedHand>()) {
        RiggedHand_L = Hand_L.GetComponent<RiggedHand>();
      }
      else RiggedHand_L = Hand_L.gameObject.AddComponent<RiggedHand>();
      HandEnableDisable handEnableDisable_L = Hand_L.gameObject.AddComponent<HandEnableDisable>();
      HandTransitionBehavior_L = handEnableDisable_L;
      RiggedHand_L.Handedness = Chirality.Left;
      RiggedHand_L.SetEditorLeapPose = false;

      Transform Hand_R = AnimatorForMapping.GetBoneTransform(HumanBodyBones.RightHand);
      if (Hand_R.GetComponent<RiggedHand>()) {
        RiggedHand_R = Hand_R.GetComponent<RiggedHand>();
      }
      else RiggedHand_R = Hand_R.gameObject.AddComponent<RiggedHand>();
      HandEnableDisable handEnableDisable_R = Hand_R.gameObject.AddComponent<HandEnableDisable>();
      HandTransitionBehavior_R = handEnableDisable_R;
      RiggedHand_R.Handedness = Chirality.Right;
      RiggedHand_R.SetEditorLeapPose = false;

      //Find palms and assign to RiggedHands
      RiggedHand_L.palm = AnimatorForMapping.GetBoneTransform(HumanBodyBones.LeftHand);
      RiggedHand_R.palm = AnimatorForMapping.GetBoneTransform(HumanBodyBones.RightHand);
      RiggedHand_R.UseMetaCarpalsOnSetup = ModelHasMetaCarpals;
      RiggedHand_L.UseMetaCarpalsOnSetup = ModelHasMetaCarpals;

      findAndAssignRiggedFingers(ModelHasMetaCarpals);

      RiggedHand_L.AutoRigRiggedHand(RiggedHand_L.palm, RiggedFinger_L_Pinky.transform, RiggedFinger_L_Index.transform);
      RiggedHand_R.AutoRigRiggedHand(RiggedHand_R.palm, RiggedFinger_R_Pinky.transform, RiggedFinger_R_Index.transform);
      if (ModelGroupName == "" || ModelGroupName != null) {
        ModelGroupName = transform.name;
      }
      HandPoolToPopulate.AddNewGroup(ModelGroupName, RiggedHand_L, RiggedHand_R);

      modelFingerPointing_L = RiggedHand_L.modelFingerPointing;
      modelPalmFacing_L = RiggedHand_L.modelPalmFacing;
      modelFingerPointing_R = RiggedHand_R.modelFingerPointing;
      modelPalmFacing_R = RiggedHand_R.modelPalmFacing;
      //AutoRigUpperBody();
    }

    //Find Fingers and assign RiggedFingers
    private void findAndAssignRiggedFingers(bool useMetaCarpals) {
      if (!useMetaCarpals) {
        RiggedFinger_L_Thumb = AnimatorForMapping.GetBoneTransform(HumanBodyBones.LeftThumbProximal).gameObject.AddComponent<RiggedFinger>();
        RiggedFinger_L_Index = AnimatorForMapping.GetBoneTransform(HumanBodyBones.LeftIndexProximal).gameObject.AddComponent<RiggedFinger>();
        RiggedFinger_L_Mid = AnimatorForMapping.GetBoneTransform(HumanBodyBones.LeftMiddleProximal).gameObject.AddComponent<RiggedFinger>();
        RiggedFinger_L_Ring = AnimatorForMapping.GetBoneTransform(HumanBodyBones.LeftRingProximal).gameObject.AddComponent<RiggedFinger>();
        RiggedFinger_L_Pinky = AnimatorForMapping.GetBoneTransform(HumanBodyBones.LeftLittleProximal).gameObject.AddComponent<RiggedFinger>();
        RiggedFinger_R_Thumb = AnimatorForMapping.GetBoneTransform(HumanBodyBones.RightThumbProximal).gameObject.AddComponent<RiggedFinger>();
        RiggedFinger_R_Index = AnimatorForMapping.GetBoneTransform(HumanBodyBones.RightIndexProximal).gameObject.AddComponent<RiggedFinger>();
        RiggedFinger_R_Mid = AnimatorForMapping.GetBoneTransform(HumanBodyBones.RightMiddleProximal).gameObject.AddComponent<RiggedFinger>();
        RiggedFinger_R_Ring = AnimatorForMapping.GetBoneTransform(HumanBodyBones.RightRingProximal).gameObject.AddComponent<RiggedFinger>();
        RiggedFinger_R_Pinky = AnimatorForMapping.GetBoneTransform(HumanBodyBones.RightLittleProximal).gameObject.AddComponent<RiggedFinger>();
      }
      else {
        RiggedFinger_L_Thumb = AnimatorForMapping.GetBoneTransform(HumanBodyBones.LeftThumbProximal).gameObject.AddComponent<RiggedFinger>();
        RiggedFinger_L_Index = AnimatorForMapping.GetBoneTransform(HumanBodyBones.LeftIndexProximal).gameObject.transform.parent.gameObject.AddComponent<RiggedFinger>();
        RiggedFinger_L_Mid = AnimatorForMapping.GetBoneTransform(HumanBodyBones.LeftMiddleProximal).gameObject.transform.parent.gameObject.AddComponent<RiggedFinger>();
        RiggedFinger_L_Ring = AnimatorForMapping.GetBoneTransform(HumanBodyBones.LeftRingProximal).gameObject.transform.parent.gameObject.AddComponent<RiggedFinger>();
        RiggedFinger_L_Pinky = AnimatorForMapping.GetBoneTransform(HumanBodyBones.LeftLittleProximal).gameObject.transform.parent.gameObject.AddComponent<RiggedFinger>();
        RiggedFinger_R_Thumb = AnimatorForMapping.GetBoneTransform(HumanBodyBones.RightThumbProximal).gameObject.AddComponent<RiggedFinger>();
        RiggedFinger_R_Index = AnimatorForMapping.GetBoneTransform(HumanBodyBones.RightIndexProximal).gameObject.transform.parent.gameObject.AddComponent<RiggedFinger>();
        RiggedFinger_R_Mid = AnimatorForMapping.GetBoneTransform(HumanBodyBones.RightMiddleProximal).gameObject.transform.parent.gameObject.AddComponent<RiggedFinger>();
        RiggedFinger_R_Ring = AnimatorForMapping.GetBoneTransform(HumanBodyBones.RightRingProximal).gameObject.transform.parent.gameObject.AddComponent<RiggedFinger>();
        RiggedFinger_R_Pinky = AnimatorForMapping.GetBoneTransform(HumanBodyBones.RightLittleProximal).gameObject.transform.parent.gameObject.AddComponent<RiggedFinger>();
      }
      RiggedFinger_L_Thumb.fingerType = Finger.FingerType.TYPE_THUMB;
      RiggedFinger_L_Index.fingerType = Finger.FingerType.TYPE_INDEX;
      RiggedFinger_L_Mid.fingerType = Finger.FingerType.TYPE_MIDDLE;
      RiggedFinger_L_Ring.fingerType = Finger.FingerType.TYPE_RING;
      RiggedFinger_L_Pinky.fingerType = Finger.FingerType.TYPE_PINKY;
      RiggedFinger_R_Thumb.fingerType = Finger.FingerType.TYPE_THUMB;
      RiggedFinger_R_Index.fingerType = Finger.FingerType.TYPE_INDEX;
      RiggedFinger_R_Mid.fingerType = Finger.FingerType.TYPE_MIDDLE;
      RiggedFinger_R_Ring.fingerType = Finger.FingerType.TYPE_RING;
      RiggedFinger_R_Pinky.fingerType = Finger.FingerType.TYPE_PINKY;
    }

    /**Removes existing RiggedFinger components so the auto rigging process can be rerun. */
    void Reset() {
      RiggedFinger[] riggedFingers = GetComponentsInChildren<RiggedFinger>();
      foreach (RiggedFinger finger in riggedFingers) {
        DestroyImmediate(finger);
      }
      DestroyImmediate(RiggedHand_L);
      DestroyImmediate(RiggedHand_R);
      DestroyImmediate(HandTransitionBehavior_L);
      DestroyImmediate(HandTransitionBehavior_R);
      if (HandPoolToPopulate != null) {
        HandPoolToPopulate.RemoveGroup(ModelGroupName);
      }
    }
    [ContextMenu("AutoRigArms")]
    public void AutoRigArms() {
      VRHeightOffset vRHeightOffset = GameObject.FindObjectOfType<VRHeightOffset>();
      float characterHeadHeight = AnimatorForMapping.GetBoneTransform(HumanBodyBones.Head).transform.position.y;
      vRHeightOffset._deviceOffsets[0].HeightOffset = characterHeadHeight + .1f;//small offset above head for eye height 
      AnimatorForMapping.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>("LeapIKArmsController");
      ikMarkersAssemblyPrefab = Resources.Load<IKMarkersAssembly>("IKMarkersAssembly");
      iKMarkersAssembly = GameObject.Instantiate(ikMarkersAssemblyPrefab) as IKMarkersAssembly;
      iKMarkersAssembly.transform.parent = transform;
      iKMarkersAssembly.transform.localPosition = new Vector3(0, 0, 0);

      if (HandTransitionBehavior_L != null) {
        DestroyImmediate(HandTransitionBehavior_L);
      }
      WristLeapToIKBlend WristLeapToIKBlend_L = RiggedHand_L.gameObject.AddComponent<WristLeapToIKBlend>();
      HandTransitionBehavior_L = WristLeapToIKBlend_L;
      WristLeapToIKBlend_L.m_IKMarkerAssembly = iKMarkersAssembly;
      WristLeapToIKBlend_L.AssignIKMarkers();
      HandTransitionBehavior_L.OnSetup();

      if (HandTransitionBehavior_R != null) {
        DestroyImmediate(HandTransitionBehavior_R);
      }
      WristLeapToIKBlend WristLeapToIKBlend_R = RiggedHand_R.gameObject.AddComponent<WristLeapToIKBlend>();
      HandTransitionBehavior_R = WristLeapToIKBlend_R;
      WristLeapToIKBlend_R.m_IKMarkerAssembly = iKMarkersAssembly;
      WristLeapToIKBlend_R.AssignIKMarkers();
      HandTransitionBehavior_R.OnSetup();

      OnAnimatorIKCaller onAnimatorIKCaller =  gameObject.AddComponent<OnAnimatorIKCaller>();
      onAnimatorIKCaller.wristLeapToIKBlend_L = (WristLeapToIKBlend)HandTransitionBehavior_L;
      onAnimatorIKCaller.wristLeapToIKBlend_R = (WristLeapToIKBlend)HandTransitionBehavior_R;

      WristLeapToIKBlend_L.ArmDropDuration = .001f;
      WristLeapToIKBlend_R.ArmDropDuration = .001f;

      //Assign and configure Animator Controller to Animator
      //AnimatorForMapping.runtimeAnimatorController = runtimeAnimatorController;
      AnimatorForMapping.updateMode = AnimatorUpdateMode.AnimatePhysics;
      AnimatorForMapping.cullingMode = AnimatorCullingMode.AlwaysAnimate;
    }
    [ContextMenu("AutoRigUpperBody")]
    public void AutoRigUpperBody() {
      AnimatorForMapping.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>("LeapUpperBodyController");
      SpineFollowTargetBehavior spineFollowTargetBehavior = gameObject.AddComponent<SpineFollowTargetBehavior>();
      WristLeapToIKBlend WristLeapToIKBlend_L = RiggedHand_L.gameObject.GetComponent<WristLeapToIKBlend>();
      WristLeapToIKBlend WristLeapToIKBlend_R = RiggedHand_R.gameObject.GetComponent<WristLeapToIKBlend>();
      WristLeapToIKBlend_L.ArmDropDuration = 1.5f;
      WristLeapToIKBlend_R.ArmDropDuration = 1.5f;
      spineFollowTargetBehavior.wristLeapToIKBlend_L = WristLeapToIKBlend_L;
      spineFollowTargetBehavior.wristLeapToIKBlend_R = WristLeapToIKBlend_R;
      ShoulderTurnBehavior shoulderTurnBehavior = gameObject.AddComponent<ShoulderTurnBehavior>();
    }
    [ContextMenu("AutoRigLocomotion")]
    public void AutoRigLocomotion() {
      AnimatorForMapping.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>("LeapAvatarController");
      LocomotionAvatar locomotionAvatar = gameObject.AddComponent<LocomotionAvatar>();
      SpineFollowTargetBehavior spineFollowTargetBehavior = gameObject.GetComponent<SpineFollowTargetBehavior>();
      spineFollowTargetBehavior.AlwaysDriveSpine = false;
    }

    public void PushVectorValues() {
      //push palm and finger facing values to RiggedHand's and RiggedFinger's
      if (RiggedHand_L) {
        RiggedHand_L.modelFingerPointing = modelFingerPointing_L;
        RiggedHand_L.modelPalmFacing = modelPalmFacing_L;
      }
      if (RiggedHand_R) {
        RiggedHand_R.modelFingerPointing = modelFingerPointing_R;
        RiggedHand_R.modelPalmFacing = modelPalmFacing_R;
      }
      if (RiggedFinger_L_Thumb) {
        RiggedFinger_L_Thumb.modelFingerPointing = modelFingerPointing_L;
        RiggedFinger_L_Thumb.modelPalmFacing = modelPalmFacing_L;
      }
      if (RiggedFinger_L_Index) {
        RiggedFinger_L_Index.modelFingerPointing = modelFingerPointing_L;
        RiggedFinger_L_Index.modelPalmFacing = modelPalmFacing_L;
      }
      if (RiggedFinger_L_Mid) {
        RiggedFinger_L_Mid.modelFingerPointing = modelFingerPointing_L;
        RiggedFinger_L_Mid.modelPalmFacing = modelPalmFacing_L;
      }
      if (RiggedFinger_L_Ring) {
        RiggedFinger_L_Ring.modelFingerPointing = modelFingerPointing_L;
        RiggedFinger_L_Ring.modelPalmFacing = modelPalmFacing_L;
      }
      if (RiggedFinger_L_Pinky) {
        RiggedFinger_L_Pinky.modelFingerPointing = modelFingerPointing_L;
        RiggedFinger_L_Pinky.modelPalmFacing = modelPalmFacing_L;
      }
      if (RiggedFinger_R_Thumb) {
        RiggedFinger_R_Thumb.modelFingerPointing = modelFingerPointing_R;
        RiggedFinger_R_Thumb.modelPalmFacing = modelPalmFacing_R;
      }
      if (RiggedFinger_R_Index) {
        RiggedFinger_R_Index.modelFingerPointing = modelFingerPointing_R;
        RiggedFinger_R_Index.modelPalmFacing = modelPalmFacing_R;
      }
      if (RiggedFinger_R_Mid) {
        RiggedFinger_R_Mid.modelFingerPointing = modelFingerPointing_R;
        RiggedFinger_R_Mid.modelPalmFacing = modelPalmFacing_R;
      }
      if (RiggedFinger_R_Ring) {
        RiggedFinger_R_Ring.modelFingerPointing = modelFingerPointing_R;
        RiggedFinger_R_Ring.modelPalmFacing = modelPalmFacing_R;
      }
      if (RiggedFinger_R_Pinky) {
        RiggedFinger_R_Pinky.modelFingerPointing = modelFingerPointing_R;
        RiggedFinger_R_Pinky.modelPalmFacing = modelPalmFacing_R;
      }
    }

    //Monobehavior's OnValidate() is used to push LeapHandsAutoRig values to RiggedHand and RiggedFinger components
    void OnValidate() {
      if (FlipPalms != flippedPalmsState) {
        modelPalmFacing_L = modelPalmFacing_L * -1f;
        modelPalmFacing_R = modelPalmFacing_R * -1f;
        flippedPalmsState = FlipPalms;
        PushVectorValues();
      }
    }
    /**Removes the ModelGroup from HandPool that corresponds to this instance of LeapHandsAutoRig */
    void OnDestroy() {
      if (HandPoolToPopulate != null) {
        HandPoolToPopulate.RemoveGroup(ModelGroupName);
      }
    }
  }
}
