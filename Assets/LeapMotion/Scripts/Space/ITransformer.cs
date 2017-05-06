/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;

namespace Leap.Unity.Space {

  public interface ITransformer {

    LeapSpaceAnchor anchor { get; }

    /// <summary>
    /// Transform a point from rect space to warped space.
    /// </summary>
    Vector3 TransformPoint(Vector3 localRectPos);

    /// <summary>
    /// Transform a point from warped space to rect space.
    /// </summary>
    Vector3 InverseTransformPoint(Vector3 localWarpedSpace);

    /// <summary>
    /// Transform a rotation from rect space to warped space.
    /// </summary>
    Quaternion TransformRotation(Vector3 localRectPos, Quaternion localRectRot);

    /// <summary>
    /// Transform a rotation from warped space to rect space.
    /// </summary>
    Quaternion InverseTransformRotation(Vector3 localWarpedPos, Quaternion localWarpedRot);

    /// <summary>
    /// Transform a direction from rect space to warped space.
    /// </summary>
    Vector3 TransformDirection(Vector3 localRectPos, Vector3 localRectDirection);

    /// <summary>
    /// Transform a direction from warped space to rect space.
    /// </summary>
    Vector3 InverseTransformDirection(Vector3 localWarpedSpace, Vector3 localWarpedDirection);

    /// <summary>
    /// Get a transformation matrix that maps a position in rect space 
    /// to a position in warped space.
    /// </summary>
    Matrix4x4 GetTransformationMatrix(Vector3 localRectPos);
  }

  public class IdentityTransformer : ITransformer {
    public static readonly IdentityTransformer single = new IdentityTransformer();

    public LeapSpaceAnchor anchor {
      get {
        return null;
      }
    }

    public Vector3 TransformPoint(Vector3 localRectPos) {
      return localRectPos;
    }

    public Vector3 InverseTransformPoint(Vector3 localWarpedSpace) {
      return localWarpedSpace;
    }

    public Quaternion TransformRotation(Vector3 localRectPos, Quaternion localRectRot) {
      return localRectRot;
    }

    public Quaternion InverseTransformRotation(Vector3 localWarpedPos, Quaternion localWarpedRot) {
      return localWarpedRot;
    }

    public Vector3 TransformDirection(Vector3 localRectPos, Vector3 localRectDirection) {
      return localRectDirection;
    }

    public Vector3 InverseTransformDirection(Vector3 localWarpedSpace, Vector3 localWarpedDirection) {
      return localWarpedDirection;
    }

    public Matrix4x4 GetTransformationMatrix(Vector3 localRectPos) {
      return Matrix4x4.TRS(localRectPos, Quaternion.identity, Vector3.one);
    }
  }
}
