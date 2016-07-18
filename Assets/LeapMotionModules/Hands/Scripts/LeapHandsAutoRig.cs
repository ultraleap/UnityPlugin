using UnityEngine;
using System.Collections;

namespace Leap.Unity {
  [ExecuteInEditMode]
  [AddComponentMenu("Leap/Auto Rig Hands")]
  public class LeapHandsAutoRig : MonoBehaviour {
    public HandPool HandPoolToPopulate;
    public Animator AnimatorForMapping;

    public string ModelGroupName = null;
    public bool UseMetaCarpals;

    [Header("RiggedHand Components")]
    public RiggedHand RiggedHand_L;
    public RiggedHand RiggedHand_R;
    [Header("HandTransitionBehavior Components")]
    public HandTransitionBehavior HandTransitionBehavior_L;
    public HandTransitionBehavior HandTransitionBehavior_R;
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

    //Skinnedmeshes are needed for storing Start Pose
    [Header("Skinned Meshes for Hands")]
    public SkinnedMeshRenderer HandMesh_Left;
    public SkinnedMeshRenderer HandMesh_Right;

    [ContextMenu("AutoRig")]
    public void AutoRig() {
      HandPoolToPopulate = GameObject.FindObjectOfType<HandPool>();
      AnimatorForMapping = gameObject.GetComponent<Animator>();
      if (AnimatorForMapping != null) {
        if (AnimatorForMapping.isHuman == true) {
          AutoRigMecanim();
          AutoAssignSkinnedMesh();
          return;
        }
        else {
          Debug.LogWarning("The Mecanim Avatar for this asset does not contain a valid IsHuman definition.  Attempting to auto map by name.");
        }
      }
      AutoRigByName();
      AutoAssignSkinnedMesh();
    }
    public void AutoAssignSkinnedMesh() {
      SkinnedMeshRenderer[] skinnedMeshesInChildren = GetComponentsInChildren<SkinnedMeshRenderer>();
      if (skinnedMeshesInChildren.Length > 1) {
        Debug.LogWarning("Since there are more than one skinned meshes in the hierarchy, please manually assign skinned meshes for each hand in the LeapHandsAutoRig component.");
        return;
      }
      if(skinnedMeshesInChildren.Length == 1){
        HandMesh_Left = skinnedMeshesInChildren[0];
        HandMesh_Right = skinnedMeshesInChildren[0];
        if (RiggedHand_L) {
          RiggedHand_L.HandMesh = HandMesh_Left;
        }
        if (RiggedHand_R) {
          RiggedHand_R.HandMesh = HandMesh_Right;
        }
      }
    }

    [ContextMenu("StoreStartPose")]
    public void StoreStartPose() {
      RiggedHand_L.StoreLocalRotations();
      RiggedHand_R.StoreLocalRotations();
    }
    [ContextMenu("ResetStartPose")]
    public void ResetStartPose() {
      RiggedHand_L.ResetLocalRotations();
      RiggedHand_R.ResetLocalRotations();
    }

    [ContextMenu("AutoRigByName")]
    void AutoRigByName() {
      //Assigning these here since this component gets added and used at editor time
      HandPoolToPopulate = GameObject.FindObjectOfType<HandPool>();
      Reset();

      //Find hands and assign RiggedHands
      Transform Hand_L = null;
      foreach (Transform t in transform) {
        if (t.name.Contains("Left")){
          Hand_L = t;
        }
      }
      RiggedHand_L = Hand_L.gameObject.AddComponent<RiggedHand>();
      HandTransitionBehavior_L = Hand_L.gameObject.AddComponent<HandEnableDisable>();
      RiggedHand_L.Handedness = Chirality.Left;
      RiggedHand_L.SetEditorLeapPose = false;
      RiggedHand_L.UseMetaCarpals = UseMetaCarpals;

      Transform Hand_R = null;
      foreach (Transform t in transform) {
        if(t.name.Contains("Right")){
          Hand_R = t;
        }
      }
      RiggedHand_R = Hand_R.gameObject.AddComponent<RiggedHand>();
      HandTransitionBehavior_R = Hand_R.gameObject.AddComponent<HandEnableDisable>();
      RiggedHand_R.Handedness = Chirality.Right;
      RiggedHand_R.SetEditorLeapPose = false;
      RiggedHand_R.UseMetaCarpals = UseMetaCarpals;

      //Find palms and assign to RiggedHands
      //RiggedHand_L.palm = AnimatorForMapping.GetBoneTransform(HumanBodyBones.LeftHand);
      //RiggedHand_R.palm = AnimatorForMapping.GetBoneTransform(HumanBodyBones.RightHand);
      RiggedHand_L.SetupRiggedHand();
      RiggedHand_R.SetupRiggedHand();

      if (ModelGroupName == null) {
        ModelGroupName = transform.name;
      }
      HandPoolToPopulate.AddNewGroup(ModelGroupName, RiggedHand_L, RiggedHand_R);

      RiggedFinger_L_Thumb = (RiggedFinger)RiggedHand_L.fingers[0];
      RiggedFinger_L_Index = (RiggedFinger)RiggedHand_L.fingers[1];
      RiggedFinger_L_Mid = (RiggedFinger)RiggedHand_L.fingers[2];
      RiggedFinger_L_Ring = (RiggedFinger)RiggedHand_L.fingers[3];
      RiggedFinger_L_Pinky = (RiggedFinger)RiggedHand_L.fingers[4];
      RiggedFinger_R_Thumb = (RiggedFinger)RiggedHand_R.fingers[0];
      RiggedFinger_R_Index = (RiggedFinger)RiggedHand_R.fingers[1];
      RiggedFinger_R_Mid = (RiggedFinger)RiggedHand_R.fingers[2];
      RiggedFinger_R_Ring = (RiggedFinger)RiggedHand_R.fingers[3];
      RiggedFinger_R_Pinky = (RiggedFinger)RiggedHand_R.fingers[4];

      modelFingerPointing_L = RiggedHand_L.modelFingerPointing;
      modelPalmFacing_L = RiggedHand_L.modelPalmFacing;
      modelFingerPointing_R = RiggedHand_R.modelFingerPointing;
      modelPalmFacing_R = RiggedHand_R.modelPalmFacing;
    }


    [ContextMenu("AutoRigMecanim")]
    void AutoRigMecanim() {
      //Assigning these here since this component gets added and used at editor time
      AnimatorForMapping = gameObject.GetComponent<Animator>();
      HandPoolToPopulate = GameObject.FindObjectOfType<HandPool>();
      Reset();

      //Find hands and assign RiggedHands
      Transform Hand_L = AnimatorForMapping.GetBoneTransform(HumanBodyBones.LeftHand);
      RiggedHand_L = Hand_L.gameObject.AddComponent<RiggedHand>();
      HandTransitionBehavior_L =Hand_L.gameObject.AddComponent<HandDrop>();
      RiggedHand_L.Handedness = Chirality.Left;
      RiggedHand_L.SetEditorLeapPose = false;

      Transform Hand_R = AnimatorForMapping.GetBoneTransform(HumanBodyBones.RightHand);
      RiggedHand_R = Hand_R.gameObject.AddComponent<RiggedHand>();
      HandTransitionBehavior_R = Hand_R.gameObject.AddComponent<HandDrop>();
      RiggedHand_R.Handedness = Chirality.Right;
      RiggedHand_R.SetEditorLeapPose = false;

      //Find palms and assign to RiggedHands
      RiggedHand_L.palm = AnimatorForMapping.GetBoneTransform(HumanBodyBones.LeftHand);
      RiggedHand_R.palm = AnimatorForMapping.GetBoneTransform(HumanBodyBones.RightHand);

      //Find Fingers and assign RiggedFingers
      RiggedFinger_L_Thumb = AnimatorForMapping.GetBoneTransform(HumanBodyBones.LeftThumbProximal).gameObject.AddComponent<RiggedFinger>();
      RiggedFinger_L_Thumb.fingerType = Finger.FingerType.TYPE_THUMB;
      RiggedFinger_L_Index = AnimatorForMapping.GetBoneTransform(HumanBodyBones.LeftIndexProximal).gameObject.AddComponent<RiggedFinger>();
      RiggedFinger_L_Index.fingerType = Finger.FingerType.TYPE_INDEX;
      RiggedFinger_L_Mid = AnimatorForMapping.GetBoneTransform(HumanBodyBones.LeftMiddleProximal).gameObject.AddComponent<RiggedFinger>();
      RiggedFinger_L_Mid.fingerType = Finger.FingerType.TYPE_MIDDLE;
      RiggedFinger_L_Ring = AnimatorForMapping.GetBoneTransform(HumanBodyBones.LeftRingProximal).gameObject.AddComponent<RiggedFinger>();
      RiggedFinger_L_Ring.fingerType = Finger.FingerType.TYPE_RING;
      RiggedFinger_L_Pinky = AnimatorForMapping.GetBoneTransform(HumanBodyBones.LeftLittleProximal).gameObject.AddComponent<RiggedFinger>();
      RiggedFinger_L_Pinky.fingerType = Finger.FingerType.TYPE_PINKY;
      RiggedFinger_R_Thumb = AnimatorForMapping.GetBoneTransform(HumanBodyBones.RightThumbProximal).gameObject.AddComponent<RiggedFinger>();
      RiggedFinger_R_Thumb.fingerType = Finger.FingerType.TYPE_THUMB;
      RiggedFinger_R_Index = AnimatorForMapping.GetBoneTransform(HumanBodyBones.RightIndexProximal).gameObject.AddComponent<RiggedFinger>();
      RiggedFinger_R_Index.fingerType = Finger.FingerType.TYPE_INDEX;
      RiggedFinger_R_Mid = AnimatorForMapping.GetBoneTransform(HumanBodyBones.RightMiddleProximal).gameObject.AddComponent<RiggedFinger>();
      RiggedFinger_R_Mid.fingerType = Finger.FingerType.TYPE_MIDDLE;
      RiggedFinger_R_Ring = AnimatorForMapping.GetBoneTransform(HumanBodyBones.RightRingProximal).gameObject.AddComponent<RiggedFinger>();
      RiggedFinger_R_Ring.fingerType = Finger.FingerType.TYPE_RING;
      RiggedFinger_R_Pinky = AnimatorForMapping.GetBoneTransform(HumanBodyBones.RightLittleProximal).gameObject.AddComponent<RiggedFinger>();
      RiggedFinger_R_Pinky.fingerType = Finger.FingerType.TYPE_PINKY;
      //Trigger SetupRiggedHand in RiggedHands

      RiggedHand_L.AutoRigRiggedHand(RiggedHand_L.palm, RiggedFinger_L_Pinky.transform, RiggedFinger_L_Index.transform);
      RiggedHand_R.AutoRigRiggedHand(RiggedHand_R.palm, RiggedFinger_R_Pinky.transform, RiggedFinger_R_Index.transform);
      if (ModelGroupName == null) {
        ModelGroupName = transform.name;
      }
      HandPoolToPopulate.AddNewGroup(ModelGroupName, RiggedHand_L, RiggedHand_R);

      modelFingerPointing_L = RiggedHand_L.modelFingerPointing;
      modelPalmFacing_L = RiggedHand_L.modelPalmFacing;
      modelFingerPointing_R = RiggedHand_R.modelFingerPointing;
      modelPalmFacing_R = RiggedHand_R.modelPalmFacing;
    }

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
    void OnValidate() {
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
      if (HandMesh_Left && RiggedHand_L) {
        RiggedHand_L.HandMesh = HandMesh_Left;
      }
      if (HandMesh_Right && RiggedHand_R) {
        RiggedHand_R.HandMesh = HandMesh_Right;
      }

    }

    void OnDestroy() {
      if (HandPoolToPopulate != null) {
        HandPoolToPopulate.RemoveGroup(ModelGroupName);
      }
    }
  }
}
