/******************************************************************************\
* Copyright (C) Leap Motion, Inc. 2011-2014.                                   *
* Leap Motion proprietary. Licensed under Apache 2.0                           *
* Available at http://www.apache.org/licenses/LICENSE-2.0.html                 *
\******************************************************************************/

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Leap;

namespace Leap.Unity {
  // Class to setup a rigged hand based on a model.
  public class RiggedHand : HandModel {
    public override ModelType HandModelType {
      get {
        return ModelType.Graphics;
      }
    }
    public override bool SupportsEditorPersistence() {
      return SetEditorLeapPose;
    }
    [SerializeField]
    private bool setEditorLeapPose = true;
    
    public bool SetEditorLeapPose {
      get { return setEditorLeapPose; }
      set {

        if (value == false) {
          RestoreJointsStartPose();
        }
        setEditorLeapPose = value;
      }
    }

    [Tooltip("Hands are typically rigged in 3D packages with the palm transform near the wrist. Uncheck this is your model's palm transform is at the center of the palm similar to Leap's API drives")]
    public bool ModelPalmAtLeapWrist = true;
    public bool UseMetaCarpals;
    public Vector3 modelFingerPointing = new Vector3(0, 0, 0);
    public Vector3 modelPalmFacing = new Vector3(0, 0, 0);
    [Header("Values for Stored Start Pose")]
    [SerializeField]
    private List<Transform> jointList = new List<Transform>();
    [SerializeField]
    private List<Quaternion> localRotations = new List<Quaternion>();
    [SerializeField]
    private List<Vector3> localPositions = new List<Vector3>();

    void OnValidate() {
      if (SetEditorLeapPose == setEditorLeapPose) {
        SetEditorLeapPose = setEditorLeapPose;
      }
    }
    public override void InitHand() {
      UpdateHand();
    }

    public Quaternion Reorientation() {
      return Quaternion.Inverse(Quaternion.LookRotation(modelFingerPointing, -modelPalmFacing));
    }
    public override void UpdateHand() {
      if (palm != null) {
        if (ModelPalmAtLeapWrist) {
          palm.position = GetWristPosition();
        }
        else {
          palm.position = GetPalmPosition();
          if (wristJoint) {
            wristJoint.position = GetWristPosition();
          }
        }
        palm.rotation = GetRiggedPalmRotation() * Reorientation();
      }

      if (forearm != null) {
        forearm.rotation = GetArmRotation() * Reorientation();
      }

      for (int i = 0; i < fingers.Length; ++i) {
        if (fingers[i] != null) {
          fingers[i].fingerType = (Finger.FingerType)i;
          fingers[i].UpdateFinger();
        }
      }
    }

    //These versions of GetPalmRotation & CalculateRotation return the opposite vector compared to LeapUnityExtension.CalculateRotation
    //This will be deprecated once LeapUnityExtension.CalculateRotation is flipped in the next release of LeapMotion Core Assets
    public Quaternion GetRiggedPalmRotation() {
      if (hand_ != null) {
        LeapTransform trs = hand_.Basis;
        return CalculateRotation(trs);
      }
      if (palm) {
        return palm.rotation;
      }
      return Quaternion.identity;
    }

    private Quaternion CalculateRotation(this LeapTransform trs) {
      Vector3 up = trs.yBasis.ToVector3();
      Vector3 forward = trs.zBasis.ToVector3();
      return Quaternion.LookRotation(forward, up);
    }

    [ContextMenu("Setup Rigged Hand")]
    public void SetupRiggedHand() {
      modelFingerPointing = new Vector3(0, 0, 0);
      modelPalmFacing = new Vector3(0, 0, 0);
      assignRiggedFingersByName();
      findFingerModels();
      modelPalmFacing = calculateModelPalmFacing(palm, fingers[1].transform, fingers[2].transform);
      modelFingerPointing = calculateModelFingerPointing();
      setFingerPalmFacing();
    }
    public void AutoRigRiggedHand(Transform palm, Transform finger1, Transform finger2) {
      Debug.Log("AutoRigRiggedHand()");
      modelFingerPointing = new Vector3(0, 0, 0);
      modelPalmFacing = new Vector3(0, 0, 0);
      findFingerModels();
      modelPalmFacing = calculateModelPalmFacing(palm, finger1, finger2);
      modelFingerPointing = calculateModelFingerPointing();
      setFingerPalmFacing();
    }
    private void assignRiggedFingersByName(){
      List<string> palmStrings = new List<string> { "palm", "Palm", "PALM" };
      List<string> thumbStrings = new List<string> { "thumb", "tmb", "Thumb", "THUMB" };
      List<string> indexStrings = new List<string> { "index", "idx" , "Index", "INDEX"};
      List<string> middleStrings = new List<string> { "middle", "mid", "Middle", "MIDDLE" };
      List<string> ringStrings = new List<string> { "ring", "Ring", "RING" };
      List<string> pinkyStrings = new List<string> { "pinky", "pin", "Pinky", "PINKY" };
      //find palm by name
      //Transform palm = null;
      Transform thumb = null;
      Transform index = null;
      Transform middle = null;
      Transform ring = null;
      Transform pinky = null;
      Transform[] children = transform.GetComponentsInChildren<Transform>();
      if (palmStrings.Any(w => transform.name.Contains(w)) ==true ){
        base.palm = transform;
      }
      else{
        foreach (Transform t in children) {
          if (palmStrings.Any(w => t.name.Contains(w)) == true) {
            base.palm = t;

          }
        }
 
      }
      if (!palm) {
        palm = transform;
      }
      if (palm) {
        foreach (Transform t in children) {
          RiggedFinger preExistingRiggedFinger;
          preExistingRiggedFinger = t.GetComponent<RiggedFinger>();
          if (!preExistingRiggedFinger) {
            if (thumbStrings.Any(w => t.name.Contains(w)) == true && t.parent == palm) {
              thumb = t;
              RiggedFinger newRiggedFinger = thumb.gameObject.AddComponent<RiggedFinger>();
              newRiggedFinger.fingerType = Finger.FingerType.TYPE_THUMB;
            }
            if (indexStrings.Any(w => t.name.Contains(w)) == true && t.parent == palm) {
              index = t;
              RiggedFinger newRiggedFinger = index.gameObject.AddComponent<RiggedFinger>();
              newRiggedFinger.fingerType = Finger.FingerType.TYPE_INDEX;
            }
            if (middleStrings.Any(w => t.name.Contains(w)) == true && t.parent == palm) {
              middle = t;
              RiggedFinger newRiggedFinger = middle.gameObject.AddComponent<RiggedFinger>();
              newRiggedFinger.fingerType = Finger.FingerType.TYPE_MIDDLE;
            }
            if (ringStrings.Any(w => t.name.Contains(w)) == true && t.parent == palm) {
              ring = t;
              RiggedFinger newRiggedFinger = ring.gameObject.AddComponent<RiggedFinger>();
              newRiggedFinger.fingerType = Finger.FingerType.TYPE_RING;
            }
            if (pinkyStrings.Any(w => t.name.Contains(w)) == true && t.parent == palm) {
              pinky = t;
              RiggedFinger newRiggedFinger = pinky.gameObject.AddComponent<RiggedFinger>();
              newRiggedFinger.fingerType = Finger.FingerType.TYPE_PINKY;
            }
          }
        }
      }



    }
    private void findFingerModels() {
      RiggedFinger[] fingerModelList = GetComponentsInChildren<RiggedFinger>();
      for (int i = 0; i < 5; i++) {
        int fingersIndex = fingerModelList[i].fingerType.indexOf();
        fingers[fingersIndex] = fingerModelList[i];
        fingerModelList[i].SetupRiggedFinger(UseMetaCarpals);
      }
    }
    private void setFingerPalmFacing() {
      RiggedFinger[] fingerModelList = GetComponentsInChildren<RiggedFinger>();
      for (int i = 0; i < 5; i++) {
        int fingersIndex = fingerModelList[i].fingerType.indexOf();
        fingers[fingersIndex] = fingerModelList[i];
        fingerModelList[i].modelPalmFacing = modelPalmFacing;
      }
    }

    private Vector3 calculateModelPalmFacing(Transform palm, Transform finger1, Transform finger2) {
      Vector3 a = palm.transform.InverseTransformPoint(palm.position);
      Vector3 b = palm.transform.InverseTransformPoint(finger1.position);
      Vector3 c = palm.transform.InverseTransformPoint(finger2.position);

      Vector3 side1 = b - a;
      Vector3 side2 = c - a;
      Vector3 perpendicular;

      if (Handedness == Chirality.Left) {
        perpendicular = Vector3.Cross(side2, side1);
      }
      else perpendicular = Vector3.Cross(side1, side2);
      Vector3 calculatedPalmFacing = CalculateZeroedVector(perpendicular);
      return calculatedPalmFacing * 1; //+works for Mixamo, -reversed LoPoly_Hands_Skeleton and Winston
    }

    private Vector3 calculateModelFingerPointing() {
      Vector3 distance = palm.transform.InverseTransformPoint(fingers[2].transform.GetChild(0).transform.position) - palm.transform.InverseTransformPoint(palm.position);
      Vector3 calculatedFingerPointing = CalculateZeroedVector(distance);
      return calculatedFingerPointing * -1f;
    }

    public static Vector3 CalculateZeroedVector(Vector3 vectorToZero) {
      var zeroed = new Vector3();
      float max = Mathf.Max(Mathf.Abs(vectorToZero.x), Mathf.Abs(vectorToZero.y), Mathf.Abs(vectorToZero.z));
      if (Mathf.Abs(vectorToZero.x) == max) {
        zeroed = (vectorToZero.x < 0) ? new Vector3(1, 0, 0) : new Vector3(-1, 0, 0);
      }
      if (Mathf.Abs(vectorToZero.y) == max) {
        zeroed = (vectorToZero.y < 0) ? new Vector3(0, 1, 0) : new Vector3(0, -1, 0);
      }
      if (Mathf.Abs(vectorToZero.z) == max) {
        zeroed = (vectorToZero.y < 0) ? new Vector3(0, 0, 1) : new Vector3(0, 0, -1);
      }
      return zeroed;
    }

    [ContextMenu("StoreJointsStartPose")]
    public void StoreJointsStartPose() {
      foreach (Transform t in palm.parent.GetComponentsInChildren<Transform>()) {
        jointList.Add(t);
        localRotations.Add(t.localRotation);
        localPositions.Add(t.localPosition);
      }
    }
    [ContextMenu("RestoreJointsStartPose")]
    public void RestoreJointsStartPose() {
      for (int i = 0; i < jointList.Count; i++) {
        Transform jointTrans = jointList[i];
        jointTrans.localRotation = localRotations[i];
        jointTrans.localPosition = localPositions[i];
      }
    }
  }
}
