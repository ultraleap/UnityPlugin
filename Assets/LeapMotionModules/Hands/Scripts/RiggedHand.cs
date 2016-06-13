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
      findFingerModels();
      modelPalmFacing = calculateModelPalmFacing();
      modelFingerPointing = calculateModelFingerPointing();
      setFingerPalmFacing();
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
      //return calculatedPalmFacing;
      if (Handedness == Chirality.Right) {
        return new Vector3(0, -1, 0);
      }
      else return new Vector3(0, 1, 0);
    }

    private Vector3 calculateModelFingerPointing() {
      Vector3 distance = transform.InverseTransformPoint(fingers[2].transform.GetChild(0).transform.position) - transform.InverseTransformPoint(palm.position);
      Vector3 calculatedFingerPointing = CalculateZeroedVector(distance);
      //return calculatedFingerPointing;
      if (Handedness == Chirality.Right) {
        return new Vector3(1, 0, 0);
      }
      else return new Vector3(-1, 0, 0);
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
    [SerializeField]
    private List<Quaternion> localRotations;
    [SerializeField]
    private List<Vector3> localPositions;
    [SerializeField]
    private List<UnityEngine.Matrix4x4> savedBindPoses;

    [ContextMenu("StoreLocalPosAndRot")]
    public void StoreLocalPosAndRot() {
      Debug.Log("StoreLocalPosAndRot()");
      SkinnedMeshRenderer skinnedMesh = GetComponentInChildren<SkinnedMeshRenderer>();
      Mesh mesh = skinnedMesh.sharedMesh;
      for (int i = 0; i < skinnedMesh.bones.Length; i++) {
        Transform boneTrans = skinnedMesh.bones[i];
        localRotations.Add(boneTrans.localRotation);
        localPositions.Add(boneTrans.localPosition);
      }
    }
    [ContextMenu("ResetLocalRotationsAndPositions")]
    public void ResetLocalPosAndRot() {
      Debug.Log("ResetLocalPosAndRot()");
      if (localPositions.Count > 0 && localRotations.Count > 0) {
        SkinnedMeshRenderer skinnedMesh = GetComponentInChildren<SkinnedMeshRenderer>();
        Mesh mesh = skinnedMesh.sharedMesh;
        for (int i = 0; i < skinnedMesh.bones.Length; i++) {
          Transform boneTrans = skinnedMesh.bones[i];
          boneTrans.localRotation = localRotations[i];
          boneTrans.localPosition = localPositions[i];
        }
      }
    }
    [ContextMenu("SaveBindPose")]
    public void SaveBindPose() {
      Debug.Log("SaveBindPose()");
      SkinnedMeshRenderer skinnedMesh = GetComponentInChildren<SkinnedMeshRenderer>();
      Mesh mesh = skinnedMesh.sharedMesh;
      for (int i = 0; i < skinnedMesh.bones.Length; i++) {
        savedBindPoses.Add(mesh.bindposes[i]);
      }
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
    /// <summary>
    /// Extract translation from transform matrix.
    /// </summary>
    /// <param name="matrix">Transform matrix. This parameter is passed by reference
    /// to improve performance; no changes will be made to it.</param>
    /// <returns>
    /// Translation offset.
    /// </returns>
    public static Vector3 ExtractTranslationFromMatrix(ref Matrix4x4 matrix) {
      Vector3 translate;
      translate.x = matrix.m03;
      translate.y = matrix.m13;
      translate.z = matrix.m23;
      return translate;
    }

    /// <summary>
    /// Extract rotation quaternion from transform matrix.
    /// </summary>
    /// <param name="matrix">Transform matrix. This parameter is passed by reference
    /// to improve performance; no changes will be made to it.</param>
    /// <returns>
    /// Quaternion representation of rotation transform.
    /// </returns>
    public static Quaternion ExtractRotationFromMatrix(ref Matrix4x4 matrix) {
      Vector3 forward;
      forward.x = matrix.m02;
      forward.y = matrix.m12;
      forward.z = matrix.m22;

      Vector3 upwards;
      upwards.x = matrix.m01;
      upwards.y = matrix.m11;
      upwards.z = matrix.m21;

      return Quaternion.LookRotation(forward, upwards);
    }

    /// <summary>
    /// Extract scale from transform matrix.
    /// </summary>
    /// <param name="matrix">Transform matrix. This parameter is passed by reference
    /// to improve performance; no changes will be made to it.</param>
    /// <returns>
    /// Scale vector.
    /// </returns>
    public static Vector3 ExtractScaleFromMatrix(ref Matrix4x4 matrix) {
      Vector3 scale;
      scale.x = new Vector4(matrix.m00, matrix.m10, matrix.m20, matrix.m30).magnitude;
      scale.y = new Vector4(matrix.m01, matrix.m11, matrix.m21, matrix.m31).magnitude;
      scale.z = new Vector4(matrix.m02, matrix.m12, matrix.m22, matrix.m32).magnitude;
      return scale;
    }

    /// <summary>
    /// Extract position, rotation and scale from TRS matrix.
    /// </summary>
    /// <param name="matrix">Transform matrix. This parameter is passed by reference
    /// to improve performance; no changes will be made to it.</param>
    /// <param name="localPosition">Output position.</param>
    /// <param name="localRotation">Output rotation.</param>
    /// <param name="localScale">Output scale.</param>
    public static void DecomposeMatrix(ref Matrix4x4 matrix, out Vector3 localPosition, out Quaternion localRotation, out Vector3 localScale) {
      localPosition = ExtractTranslationFromMatrix(ref matrix);
      localRotation = ExtractRotationFromMatrix(ref matrix);
      localScale = ExtractScaleFromMatrix(ref matrix);
    }

    /// <summary>
    /// Set transform component from TRS matrix.
    /// </summary>
    /// <param name="transform">Transform component.</param>
    /// <param name="matrix">Transform matrix. This parameter is passed by reference
    /// to improve performance; no changes will be made to it.</param>
    public static void SetTransformFromMatrix(Transform transform, ref Matrix4x4 matrix) {
      transform.position = ExtractTranslationFromMatrix(ref matrix);
      transform.rotation = ExtractRotationFromMatrix(ref matrix);
      transform.localScale = ExtractScaleFromMatrix(ref matrix);
    }
  }
}
