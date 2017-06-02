/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System;
using UnityEngine;

namespace Leap.Unity.Space {

  public class LeapSphericalSpace : LeapRadialSpace {

    protected override ITransformer CosntructBaseTransformer() {
      return new Transformer() {
        space = this,
        anchor = this,
        angleXOffset = 0,
        angleYOffset = 0,
        radiusOffset = radius,
        radiansPerMeter = 1.0f / radius
      };
    }

    protected override ITransformer ConstructTransformer(LeapSpaceAnchor anchor) {
      return new Transformer() {
        space = this,
        anchor = anchor
      };
    }

    protected override void UpdateRadialTransformer(ITransformer transformer, ITransformer parent, Vector3 rectSpaceDelta) {
      var radialTransformer = transformer as Transformer;
      var radialParent = parent as Transformer;

      radialTransformer.angleXOffset = radialParent.angleXOffset + rectSpaceDelta.x / radialParent.radiusOffset;
      radialTransformer.angleYOffset = radialParent.angleYOffset + rectSpaceDelta.y / radialParent.radiusOffset;
      radialTransformer.radiusOffset = radialParent.radiusOffset + rectSpaceDelta.z;
      radialTransformer.radiansPerMeter = 1.0f / (radialTransformer.radiusOffset);
    }

    public class Transformer : IRadialTransformer {
      public LeapSphericalSpace space { get; set; }
      public LeapSpaceAnchor anchor { get; set; }

      public float angleXOffset;
      public float angleYOffset;
      public float radiusOffset;
      public float radiansPerMeter;

      public Vector3 TransformPoint(Vector3 localRectPos) {
        Vector3 anchorDelta;

        Vector3 anchorRectPos = space.transform.InverseTransformPoint(anchor.transform.position);
        anchorDelta = localRectPos - anchorRectPos;

        float angleX = angleXOffset + anchorDelta.x / radiusOffset;
        float angleY = angleYOffset + anchorDelta.y / radiusOffset;
        float radius = radiusOffset + anchorDelta.z;

        Vector3 position;
        position.x = 0;
        position.y = Mathf.Sin(angleY) * radius;
        position.z = Mathf.Cos(angleY) * radius;

        Vector3 position2;
        position2.x = Mathf.Sin(angleX) * position.z;
        position2.y = position.y;
        position2.z = Mathf.Cos(angleX) * position.z - space.radius;

        return position2;
      }

      public Vector3 InverseTransformPoint(Vector3 localWarpedPos) {
        localWarpedPos.z += space.radius;

        Vector3 preRotatedPos;
        preRotatedPos.x = 0;
        preRotatedPos.y = localWarpedPos.y;
        preRotatedPos.z = new Vector2(localWarpedPos.x, localWarpedPos.z).magnitude;

        float angleX = Mathf.Atan2(localWarpedPos.x, localWarpedPos.z);
        float angleY = Mathf.Atan2(preRotatedPos.y, preRotatedPos.z);
        float radius = new Vector2(preRotatedPos.z, preRotatedPos.y).magnitude;

        Vector3 anchorDelta;
        anchorDelta.x = (angleX - angleXOffset) * radiusOffset;
        anchorDelta.y = (angleY - angleYOffset) * radiusOffset;
        anchorDelta.z = radius - radiusOffset;

        Vector3 anchorRectPos = space.transform.InverseTransformPoint(anchor.transform.position);
        Vector3 localRectPos = anchorRectPos + anchorDelta;

        return localRectPos;
      }

      public Quaternion TransformRotation(Vector3 localRectPos, Quaternion localRectRot) {
        Vector3 anchorDelta;

        Vector3 anchorRectPos = space.transform.InverseTransformPoint(anchor.transform.position);
        anchorDelta = localRectPos - anchorRectPos;

        float angleX = angleXOffset + anchorDelta.x / radiusOffset;
        float angleY = angleYOffset + anchorDelta.y / radiusOffset;

        Quaternion rotation = Quaternion.Euler(-angleY * Mathf.Rad2Deg,
                                                angleX * Mathf.Rad2Deg,
                                                0);

        return rotation * localRectRot;
      }

      public Quaternion InverseTransformRotation(Vector3 localWarpedPos, Quaternion localWarpedRot) {
        localWarpedPos.z += space.radius;

        Vector3 preRotatedPos;
        preRotatedPos.x = 0;
        preRotatedPos.y = localWarpedPos.y;
        preRotatedPos.z = new Vector2(localWarpedPos.x, localWarpedPos.z).magnitude;

        float angleX = Mathf.Atan2(localWarpedPos.x, localWarpedPos.z);
        float angleY = Mathf.Atan2(preRotatedPos.y, preRotatedPos.z);

        Quaternion baseRot = Quaternion.Euler(-angleY * Mathf.Rad2Deg,
                                      angleX * Mathf.Rad2Deg,
                                      0);
        Quaternion invRot = Quaternion.Inverse(baseRot);

        return invRot * localWarpedRot;
      }

      public Vector3 TransformDirection(Vector3 localRectPos, Vector3 localRectDirection) {
        Vector3 anchorDelta;

        Vector3 anchorRectPos = space.transform.InverseTransformPoint(anchor.transform.position);
        anchorDelta = localRectPos - anchorRectPos;

        float angleX = angleXOffset + anchorDelta.x / radiusOffset;
        float angleY = angleYOffset + anchorDelta.y / radiusOffset;

        Quaternion rotation = Quaternion.Euler(-angleY * Mathf.Rad2Deg,
                                                angleX * Mathf.Rad2Deg,
                                                0);

        return rotation * localRectDirection;
      }

      public Vector3 InverseTransformDirection(Vector3 localWarpedPos, Vector3 localWarpedDirection) {
        localWarpedPos.z += space.radius;

        Vector3 preRotatedPos;
        preRotatedPos.x = 0;
        preRotatedPos.y = localWarpedPos.y;
        preRotatedPos.z = new Vector2(localWarpedPos.x, localWarpedPos.z).magnitude;

        float angleX = Mathf.Atan2(localWarpedPos.x, localWarpedPos.z);
        float angleY = Mathf.Atan2(preRotatedPos.y, preRotatedPos.z);

        Quaternion baseRot = Quaternion.Euler(-angleY * Mathf.Rad2Deg,
                                              angleX * Mathf.Rad2Deg,
                                              0);
        Quaternion invRot = Quaternion.Inverse(baseRot);
        return invRot * localWarpedDirection;
      }

      public Matrix4x4 GetTransformationMatrix(Vector3 localRectPos) {
        Vector3 anchorDelta;

        Vector3 anchorRectPos = space.transform.InverseTransformPoint(anchor.transform.position);
        anchorDelta = localRectPos - anchorRectPos;

        float angleX = angleXOffset + anchorDelta.x / radiusOffset;
        float angleY = angleYOffset + anchorDelta.y / radiusOffset;
        float radius = radiusOffset + anchorDelta.z;

        Vector3 position;
        position.x = 0;
        position.y = Mathf.Sin(angleY) * radius;
        position.z = Mathf.Cos(angleY) * radius;

        Vector3 position2;
        position2.x = Mathf.Sin(angleX) * position.z;
        position2.y = position.y;
        position2.z = Mathf.Cos(angleX) * position.z - space.radius;

        Quaternion rotation = Quaternion.Euler(-angleY * Mathf.Rad2Deg,
                                                angleX * Mathf.Rad2Deg,
                                                0);

        return Matrix4x4.TRS(position2, rotation, Vector3.one);
      }

      public Vector4 GetVectorRepresentation(Transform element) {
        Vector3 elementRectPos = space.transform.InverseTransformPoint(element.position);
        Vector3 anchorRectPos = space.transform.InverseTransformPoint(anchor.transform.position);
        Vector3 delta = elementRectPos - anchorRectPos;

        Vector4 rep;
        rep.x = angleXOffset + delta.x / radiusOffset;
        rep.y = angleYOffset + delta.y / radiusOffset;
        rep.z = radiusOffset + delta.z;
        rep.w = 1.0f / radiusOffset;
        return rep;
      }
    }
  }
}
