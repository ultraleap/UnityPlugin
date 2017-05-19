/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
using System.Collections;
using Leap;

namespace Leap.Unity {
  // Class to setup a rigged hand based on a model.
  public class MobileRiggedHand : HandModel {
    public override ModelType HandModelType {
      get {
        return ModelType.Graphics;
      }
    }
    public override bool SupportsEditorPersistence() {
      return true;
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

    private Quaternion CalculateRotation(LeapTransform trs) {
      Vector3 up = trs.yBasis.ToVector3();
      Vector3 forward = trs.zBasis.ToVector3();
      return Quaternion.LookRotation(forward, up);
    }

    [ContextMenu("Setup Rigged Hand")]
    public void SetupRiggedHand() {
      modelFingerPointing = new Vector3(0, 0, 0);
      modelPalmFacing = new Vector3(0, 0, 0);
      findFingerModels();
      modelPalmFacing = calculateModelPalmFacing();
      modelFingerPointing = calculateModelFingerPointing();
      setFingerPalmFacing();
    }

    private void findFingerModels() {
	  MobileRiggedFinger[] fingerModelList = GetComponentsInChildren<MobileRiggedFinger>();
      for (int i = 0; i < 5; i++) {
        int fingersIndex = fingerModelList[i].fingerType.indexOf();
        fingers[fingersIndex] = fingerModelList[i];
        fingerModelList[i].SetupRiggedFinger(UseMetaCarpals);
      }
    }
    private void setFingerPalmFacing() {
	  MobileRiggedFinger[] fingerModelList = GetComponentsInChildren<MobileRiggedFinger>();
      for (int i = 0; i < 5; i++) {
        int fingersIndex = fingerModelList[i].fingerType.indexOf();
        fingers[fingersIndex] = fingerModelList[i];
        fingerModelList[i].modelPalmFacing = modelPalmFacing;
      }
    }

    private Vector3 calculateModelPalmFacing() {
      Vector3 a = transform.InverseTransformPoint(palm.position);
      Vector3 b = transform.InverseTransformPoint(fingers[2].transform.position);
      Vector3 c = transform.InverseTransformPoint(fingers[1].transform.position);

      Vector3 side1 = b - a;
      Vector3 side2 = c - a;
      Vector3 perpendicular;

      if (Handedness == Chirality.Left) {
        perpendicular = Vector3.Cross(side2, side1);
      }
      else perpendicular = Vector3.Cross(side1, side2);
      Vector3 calculatedPalmFacing = CalculateZeroedVector(perpendicular);
      return calculatedPalmFacing;
    }

    private Vector3 calculateModelFingerPointing() {
      Vector3 distance = transform.InverseTransformPoint(fingers[2].transform.GetChild(0).transform.position) - transform.InverseTransformPoint(palm.position);
      Vector3 calculatedFingerPointing = CalculateZeroedVector(distance);
      return calculatedFingerPointing;
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
  }
}
