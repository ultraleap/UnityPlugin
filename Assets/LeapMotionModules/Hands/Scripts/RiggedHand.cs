/******************************************************************************\
* Copyright (C) Leap Motion, Inc. 2011-2014.                                   *
* Leap Motion proprietary. Licensed under Apache 2.0                           *
* Available at http://www.apache.org/licenses/LICENSE-2.0.html                 *
\******************************************************************************/

using UnityEngine;
using System.Collections;
using Leap;

namespace Leap.Unity {
  // Class to setup a rigged hand based on a model.
  public class RiggedHand : HandModel {
    public override ModelType HandModelType {
      get {
        return ModelType.Graphics;
      }
    }
    public Vector3 modelFingerPointing = new Vector3(0, 0, 0);//Vector3.forward;
    public Vector3 modelPalmFacing = new Vector3(0, 0, 0);// -Vector3.up;

    public override void InitHand() {
      UpdateHand();
    }

    public Quaternion Reorientation() {
      return Quaternion.Inverse(Quaternion.LookRotation(modelFingerPointing, -modelPalmFacing));
    }
    

    public override void UpdateHand() {
      if (palm != null) {
        palm.position = GetWristPosition();
        if (wristJoint) {
          wristJoint.position = GetWristPosition();
        }
        else palm.position = GetWristPosition();
        palm.rotation = GetPalmRotation() * Reorientation();
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
    [ContextMenu("Setup Rigged Hand")]
    public void SetupRiggedHand() {
      modelFingerPointing = new Vector3(0, 0, 0);
      modelPalmFacing = new Vector3(0, 0, 0);
      modelPalmFacing = calculateModelPalmFacing();
      modelFingerPointing = calculateModelFingerPointing();
      findFingerModels();
    }

    private void findFingerModels() {
      RiggedFinger[] fingerModelList = GetComponentsInChildren<RiggedFinger>();
      for (int i = 0; i < 5; i++ ) {
        int fingersIndex = fingerModelList[i].fingerType.indexOf();
        fingers[fingersIndex] = fingerModelList[i];
        fingerModelList[i].SetupRiggedFinger();
        fingerModelList[i].modelPalmFacing = modelPalmFacing;
      }
    }
    private Vector3 calculateModelPalmFacing() {
      Vector3 a = transform.InverseTransformPoint(palm.position);
      Vector3 b = transform.InverseTransformPoint(palm.transform.GetChild(1).transform.position);
      Vector3 c = transform.InverseTransformPoint(palm.transform.GetChild(2).transform.position);

      Vector3 side1 = b - a;
      Vector3 side2 = c - a;
      Vector3 perpendicular = Vector3.Cross(side1, side2);
      Vector3 calculatedPalmFacing = CalculateZeroedVector(perpendicular);
      if (Handedness == Chirality.Left) {
        calculatedPalmFacing = calculatedPalmFacing * -1f;
      }
      return calculatedPalmFacing;
    }
    private Vector3 calculateModelFingerPointing() {
      Vector3 distance =  palm.transform.InverseTransformPoint(palm.transform.GetChild(2).transform.GetChild(0).transform.position) - palm.localPosition;
      Vector3 calculatedFingerPointing = CalculateZeroedVector(distance);
      return calculatedFingerPointing;
    }
    public Vector3 CalculateZeroedVector(Vector3 vectorToZero) {
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
