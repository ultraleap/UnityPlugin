/******************************************************************************\
* Copyright (C) Leap Motion, Inc. 2011-2014.                                   *
* Leap Motion proprietary. Licensed under Apache 2.0                           *
* Available at http://www.apache.org/licenses/LICENSE-2.0.html                 *
\******************************************************************************/

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
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
          //ResetToBindPose();
        }
        setEditorLeapPose = value;
      }
    }
    void OnValidate() {
      if (SetEditorLeapPose == setEditorLeapPose) {
        SetEditorLeapPose = setEditorLeapPose;
      }
    }

    [Tooltip("Hands are typically rigged in 3D packages with the palm transform near the wrist. Uncheck this is your model's palm transform is at the center of the palm similar to Leap's API drives")]
    public bool ModelPalmAtLeapWrist = true;
    public bool UseMetaCarpals;
    public Vector3 modelFingerPointing = new Vector3(0, 0, 0);
    public Vector3 modelPalmFacing = new Vector3(0, 0, 0);

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
    [ContextMenu("Auto Rig Hand")]
    public void AutoRigRiggedHand(Transform palm, Transform finger1, Transform finger2) {
      Debug.Log("AutoRigRiggedHand()");
      modelFingerPointing = new Vector3(0, 0, 0);
      modelPalmFacing = new Vector3(0, 0, 0);
      findFingerModels();
      modelPalmFacing = calculateModelPalmFacing(palm, finger1, finger2);
      modelFingerPointing = calculateModelFingerPointing();
      setFingerPalmFacing();
    }
    [ContextMenu("Assign Rigged Fingers")]
    private void assignRiggedFingersByName(){
      Debug.Log("1");
      //find palm by name
      //Transform palm = null;
      Transform thumb = null;
      Transform index = null;
      Transform middle = null;
      Transform ring = null;
      Transform pinky = null;
      Transform[] children = transform.GetComponentsInChildren<Transform>();
      if(transform.name.Contains("Palm")){
        Debug.Log("2");

        base.palm = transform;
      }
      else{
        Debug.Log("3");

        foreach (Transform t in children) {
          if (t.name.Contains("Palm")) {
            base.palm = t;

          }
        }
 
      }
      if (!palm) {
        palm = transform;
      }
      if (palm) {
        foreach (Transform t in children) {
          if (t.name.Contains("thumb") && t.parent == palm) {
            thumb = t;
            RiggedFinger newRiggedFinger = thumb.gameObject.AddComponent<RiggedFinger>();
            newRiggedFinger.fingerType = Finger.FingerType.TYPE_THUMB;
          }
          if (t.name.Contains("index") && t.parent == palm) {
            index = t;
            RiggedFinger newRiggedFinger = index.gameObject.AddComponent<RiggedFinger>();
            newRiggedFinger.fingerType = Finger.FingerType.TYPE_INDEX;
          }
          if (t.name.Contains("middle") && t.parent == palm) {
            middle = t;
            RiggedFinger newRiggedFinger = middle.gameObject.AddComponent<RiggedFinger>();
            newRiggedFinger.fingerType = Finger.FingerType.TYPE_MIDDLE;
          }
          if (t.name.Contains("ring") && t.parent == palm) {
            ring = t;
            RiggedFinger newRiggedFinger = ring.gameObject.AddComponent<RiggedFinger>();
            newRiggedFinger.fingerType = Finger.FingerType.TYPE_RING;
          }
          if (t.name.Contains("pinky") && t.parent == palm) {
            pinky = t;
            RiggedFinger newRiggedFinger = pinky.gameObject.AddComponent<RiggedFinger>();
            newRiggedFinger.fingerType = Finger.FingerType.TYPE_PINKY;
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
      return calculatedPalmFacing * 1; //works for suit01, reversed for beta & LoPoly_Hands
      //if (Handedness == Chirality.Right) {
      //  return new Vector3(0, -1, 0);
      //}
      //else {
      //  return new Vector3(0, 1, 0);
      //}
    }

    private Vector3 calculateModelFingerPointing() {
      Vector3 distance = palm.transform.InverseTransformPoint(fingers[2].transform.GetChild(0).transform.position) - palm.transform.InverseTransformPoint(palm.position);
      Vector3 calculatedFingerPointing = CalculateZeroedVector(distance);
      //reversed if using SetupRiggedHand on separate LoPoly_Hands
      return calculatedFingerPointing * -1f;

      //Hard wired vectors below are Reversed between suit01 and LoPoly_Hands_Skeleton
      //if (Handedness == Chirality.Right) {
      //  return new Vector3(-1, 0, 0);
      //}
      //else return new Vector3(1, 0, 0);
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

    [ContextMenu("ResetToBindPose")]
    public void ResetToBindPose() {
      Debug.Log("resetToBindPose()");
      SkinnedMeshRenderer skinnedMesh = GetComponentInChildren<SkinnedMeshRenderer>();
      Mesh mesh = skinnedMesh.sharedMesh;
      for (int i = 0; i < skinnedMesh.bones.Length; i++) {
        Transform boneTrans = skinnedMesh.bones[i];
        // Recreate the local transform matrix of the bone
        Matrix4x4 localMatrix = mesh.bindposes[i].inverse;
        //UnityEngine.Matrix4x4 localMatrix = savedBindPoses[i].inverse;
        //SetTransformFromMatrix(boneTrans, ref localMatrix);

        //boneTrans.position = localMatrix.MultiplyPoint(Vector3.zero);
        boneTrans.rotation = Quaternion.LookRotation(localMatrix.GetColumn(2), localMatrix.GetColumn(1));
      }
    }
  }
}
