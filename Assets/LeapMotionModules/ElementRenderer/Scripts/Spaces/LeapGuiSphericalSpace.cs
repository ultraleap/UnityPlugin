using System;
using UnityEngine;

[AddComponentMenu("")]
[LeapGuiTag("Spherical")]
public class LeapGuiSphericalSpace : LeapGuiRadialSpace<LeapGuiSphericalSpace.Transformer> {
  public const string FEATURE_NAME = LeapGui.FEATURE_PREFIX + "SPHERICAL";

  protected override Transformer GetRootTransformer() {
    return new Transformer() {
      space = this,
      anchor = transform,
      angleXOffset = 0,
      angleYOffset = 0,
      radiusOffset = radius,
      radiansPerMeter = 1.0f / radius
    };
  }

  protected override void SetTransformerRelativeTo(Transformer target, Transformer parent, Vector3 guiSpaceDelta) {
    target.angleXOffset = parent.angleXOffset + guiSpaceDelta.x / parent.radiusOffset;
    target.angleYOffset = parent.angleYOffset + guiSpaceDelta.y / parent.radiusOffset;
    target.radiusOffset = parent.radiusOffset + guiSpaceDelta.z;
    target.radiansPerMeter = 1.0f / (target.radiusOffset);
  }

  public class Transformer : IRadialTransformer {
    public LeapGuiSphericalSpace space;
    public Transform anchor;

    public float angleXOffset;
    public float angleYOffset;
    public float radiusOffset;
    public float radiansPerMeter;

    public Vector3 TransformPoint(Vector3 localRectPos) {
      Vector3 anchorDelta;

      Vector3 anchorGuiPos = space.gui.transform.InverseTransformPoint(anchor.position);
      anchorDelta = localRectPos - anchorGuiPos;

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

    public Vector3 InverseTransformPoint(Vector3 localGuiPos) {
      throw new NotImplementedException();
    }

    public Quaternion TransformRotation(Quaternion localRectRot) {
      return Quaternion.Euler(-angleYOffset, angleXOffset, 0) * localRectRot;
    }

    public Quaternion InverseTransformRotation(Quaternion localGuiRot) {
      throw new NotImplementedException();
    }

    public Vector3 TransformDirection(Vector3 direction) {
      throw new NotImplementedException();
    }

    public Vector3 InverseTransformDirection(Vector3 direction) {
      throw new NotImplementedException();
    }

    public Matrix4x4 GetTransformationMatrix(Vector3 localRectPos) {
      Vector3 anchorDelta;

      Vector3 anchorGuiPos = space.gui.transform.InverseTransformPoint(anchor.position);
      anchorDelta = localRectPos - anchorGuiPos;

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

    public Vector4 GetVectorRepresentation(LeapGuiElement element) {
      Vector3 elementGuiPos = space.gui.transform.InverseTransformPoint(element.transform.position);
      Vector3 anchorGuiPos = space.gui.transform.InverseTransformPoint(anchor.position);
      Vector3 delta = elementGuiPos - anchorGuiPos;

      Vector4 rep;
      rep.x = angleXOffset + delta.x / radiusOffset;
      rep.y = angleYOffset + delta.y / radiusOffset;
      rep.z = radiusOffset + delta.z;
      rep.w = 1.0f / radiusOffset;
      return rep;
    }
  }
}
