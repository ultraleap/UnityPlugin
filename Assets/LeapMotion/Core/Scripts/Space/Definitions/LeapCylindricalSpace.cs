/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;

namespace Leap.Unity.Space {

  public class LeapCylindricalSpace : LeapRadialSpace {

    protected override ITransformer CosntructBaseTransformer() {
      return new Transformer() {
        space = this,
        anchor = this,
        angleOffset = 0,
        heightOffset = 0,
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

      radialTransformer.angleOffset = radialParent.angleOffset + rectSpaceDelta.x / radialParent.radiusOffset;
      radialTransformer.heightOffset = radialParent.heightOffset + rectSpaceDelta.y;
      radialTransformer.radiusOffset = radialParent.radiusOffset + rectSpaceDelta.z;
      radialTransformer.radiansPerMeter = 1.0f / (radialTransformer.radiusOffset);
    }

    public class Transformer : IRadialTransformer {
      public LeapCylindricalSpace space { get; set; }
      public LeapSpaceAnchor anchor { get; set; }

      public float angleOffset;
      public float heightOffset;
      public float radiusOffset;
      public float radiansPerMeter;

      public Vector3 TransformPoint(Vector3 localRectPos) {
        Vector3 anchorDelta;

        Vector3 anchorRectPos = space.transform.InverseTransformPoint(anchor.transform.position);
        anchorDelta = localRectPos - anchorRectPos;

        float angle = angleOffset + anchorDelta.x / radiusOffset;
        float height = heightOffset + anchorDelta.y;
        float radius = radiusOffset + anchorDelta.z;

        Vector3 position;
        position.x = Mathf.Sin(angle) * radius;
        position.y = height;
        position.z = Mathf.Cos(angle) * radius - space.radius;
        return position;
      }

      public Vector3 InverseTransformPoint(Vector3 localWarpedPos) {
        localWarpedPos.z += space.radius;

        float angle = Mathf.Atan2(localWarpedPos.x, localWarpedPos.z);
        float height = localWarpedPos.y;
        float radius = new Vector2(localWarpedPos.x, localWarpedPos.z).magnitude;

        Vector3 anchorDelta;
        anchorDelta.x = (angle - angleOffset) * radiusOffset;
        anchorDelta.y = height - heightOffset;
        anchorDelta.z = radius - radiusOffset;

        Vector3 anchorRectPos = space.transform.InverseTransformPoint(anchor.transform.position);
        Vector3 localRectPos = anchorRectPos + anchorDelta;

        return localRectPos;
      }

      public Quaternion TransformRotation(Vector3 localRectPos, Quaternion localRectRot) {
        Vector3 anchorDelta;

        Vector3 anchorRectPos = space.transform.InverseTransformPoint(anchor.transform.position);
        anchorDelta = localRectPos - anchorRectPos;

        float angle = angleOffset + anchorDelta.x / radiusOffset;

        Quaternion rotation = Quaternion.Euler(0, angle * Mathf.Rad2Deg, 0);

        return rotation * localRectRot;
      }

      public Quaternion InverseTransformRotation(Vector3 localWarpedPos, Quaternion localWarpedRot) {
        localWarpedPos.z += space.radius;

        float angle = Mathf.Atan2(localWarpedPos.x, localWarpedPos.z);

        return Quaternion.Euler(0, -angle * Mathf.Rad2Deg, 0) * localWarpedRot;
      }

      public Vector3 TransformDirection(Vector3 localRectPos, Vector3 localRectDirection) {
        Vector3 anchorDelta;

        Vector3 anchorRectPos = space.transform.InverseTransformPoint(anchor.transform.position);
        anchorDelta = localRectPos - anchorRectPos;

        float angle = angleOffset + anchorDelta.x / radiusOffset;

        Quaternion rotation = Quaternion.Euler(0, angle * Mathf.Rad2Deg, 0);

        return rotation * localRectDirection;
      }

      public Vector3 InverseTransformDirection(Vector3 localWarpedPos, Vector3 localWarpedDirection) {
        localWarpedPos.z += space.radius;

        float angle = Mathf.Atan2(localWarpedPos.x, localWarpedPos.z);

        return Quaternion.Euler(0, -angle * Mathf.Rad2Deg, 0) * localWarpedDirection;
      }

      public Matrix4x4 GetTransformationMatrix(Vector3 localRectPos) {
        Vector3 anchorDelta;

        Vector3 anchorRectPos = space.transform.InverseTransformPoint(anchor.transform.position);
        anchorDelta = localRectPos - anchorRectPos;

        float angle = angleOffset + anchorDelta.x / radiusOffset;
        float height = heightOffset + anchorDelta.y;
        float radius = radiusOffset + anchorDelta.z;

        Vector3 position;
        position.x = Mathf.Sin(angle) * radius;
        position.y = height;
        position.z = Mathf.Cos(angle) * radius - space.radius;

        Quaternion rotation = Quaternion.Euler(0, angle * Mathf.Rad2Deg, 0);

        return Matrix4x4.TRS(position, rotation, Vector3.one);
      }

      public Vector4 GetVectorRepresentation(Transform element) {
        Vector3 elementRectPos = space.transform.InverseTransformPoint(element.position);
        Vector3 anchorRectPos = space.transform.InverseTransformPoint(anchor.transform.position);
        Vector3 delta = elementRectPos - anchorRectPos;

        Vector4 rep;
        rep.x = angleOffset + delta.x / radiusOffset;
        rep.y = heightOffset + delta.y;
        rep.z = radiusOffset + delta.z;
        rep.w = 1.0f / radiusOffset;
        return rep;
      }
    }
  }
}
