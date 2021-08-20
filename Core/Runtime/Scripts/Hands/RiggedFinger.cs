/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;
using System.Collections;
using Leap;

namespace Leap.Unity {

  /// <summary>
  /// Manages the position and orientation of the bones in a model rigged for skeletal
  /// animation.
  ///  
  /// The class expects that the graphics model's bones that correspond to the bones in
  /// the Leap Motion hand model are in the same order in the bones array.
  /// </summary>
  public class RiggedFinger : FingerModel {

    /// <summary>
    /// Allows the mesh to be stretched to align with finger joint positions.
    /// Only set to true when mesh is not visible.
    /// </summary>
    [HideInInspector]
    public bool deformPosition = false;

    [HideInInspector]
    public bool scaleLastFingerBone = false;

    public Vector3 modelFingerPointing = Vector3.forward;
    public Vector3 modelPalmFacing = -Vector3.up;

    public Quaternion Reorientation() {
      return Quaternion.Inverse(Quaternion.LookRotation(modelFingerPointing, -modelPalmFacing));
    }


    /// <summary>
    /// Fingertip lengths for the standard edit-time hand.
    /// </summary>
    private static float[] s_standardFingertipLengths = null;
    static RiggedFinger() {
      // Calculate standard fingertip lengths.
      s_standardFingertipLengths = new float[5];
      var testHand = TestHandFactory.MakeTestHand(isLeft: true,
                           unitType: TestHandFactory.UnitType.UnityUnits);
      for (int i = 0; i < 5; i++) {
        var fingertipBone = testHand.Fingers[i].bones[3];
        s_standardFingertipLengths[i] = fingertipBone.Length;
      }
    }

    private RiggedHand _parentRiggedHand = null;
    /// <summary>
    /// Updates model bone positions and rotations based on tracked hand data.
    /// </summary>
    public override void UpdateFinger() {
      for (int i = 0; i < bones.Length; ++i) {
        if (bones[i] != null) {
          bones[i].rotation = GetBoneRotation(i) * Reorientation();
          if (deformPosition) {
            var boneRootPos = GetJointPosition(i);
            bones[i].position = boneRootPos;

            if (i == 3 && scaleLastFingerBone) {
              // Set fingertip base bone scale to match the bone length to the fingertip.
              // This will only scale correctly if the model was constructed to match
              // the standard "test" edit-time hand model from the TestHandFactory.
              var boneTipPos = GetJointPosition(i + 1);
              var boneVec = boneTipPos - boneRootPos;

              // If the rigged hand is scaled (due to a scaled rig), we'll need to divide
              // out that scale from the bone length to get its normal length.
              if (_parentRiggedHand == null) {
                _parentRiggedHand = GetComponentInParent<RiggedHand>();
              }
              if (_parentRiggedHand != null) {
                var parentRiggedHandScale = _parentRiggedHand.transform.lossyScale.x;
                if (parentRiggedHandScale != 0f && parentRiggedHandScale != 1f) {
                  boneVec /= parentRiggedHandScale;
                }
              }

              var boneLen = boneVec.magnitude;

              var standardLen = s_standardFingertipLengths[(int)this.fingerType];
              var newScale = bones[i].transform.localScale;
              var lengthComponentIdx = getLargestComponentIndex(modelFingerPointing);
              newScale[lengthComponentIdx] = boneLen / standardLen;
              bones[i].transform.localScale = newScale;
            }
          }
        }
      }
    }

    private int getLargestComponentIndex(Vector3 pointingVector) {
      var largestValue = 0f;
      var largestIdx = 0;
      for (int i = 0; i < 3; i++) {
        var testValue = pointingVector[i];
        if (Mathf.Abs(testValue) > largestValue) {
          largestIdx = i;
          largestValue = Mathf.Abs(testValue);
        }
      }
      return largestIdx;
    }

    public void SetupRiggedFinger (bool useMetaCarpals) {
      findBoneTransforms(useMetaCarpals);
      modelFingerPointing = calulateModelFingerPointing();
    }

    private void findBoneTransforms(bool useMetaCarpals) {
      if (!useMetaCarpals || fingerType == Finger.FingerType.TYPE_THUMB) {
        bones[1] = transform;
        bones[2] = transform.GetChild(0).transform;
        bones[3] = transform.GetChild(0).transform.GetChild(0).transform;
      }
      else {
        bones[0] = transform;
        bones[1] = transform.GetChild(0).transform;
        bones[2] = transform.GetChild(0).transform.GetChild(0).transform;
        bones[3] = transform.GetChild(0).transform.GetChild(0).transform.GetChild(0).transform;

      }
    }

    private Vector3 calulateModelFingerPointing() {
      Vector3 distance = transform.InverseTransformPoint(transform.position) - transform.InverseTransformPoint(transform.GetChild(0).transform.position);
      Vector3 zeroed = RiggedHand.CalculateZeroedVector(distance);
      return zeroed;
    }

  } 
}
