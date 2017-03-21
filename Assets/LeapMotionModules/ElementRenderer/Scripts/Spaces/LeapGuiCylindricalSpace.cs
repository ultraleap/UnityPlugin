using System;
using UnityEngine;

[AddComponentMenu("")]
[LeapGuiTag("Cylindrical")]
public class LeapGuiCylindricalSpace : LeapGuiRadialSpace<LeapGuiCylindricalSpace.Transformer> {
  public const string FEATURE_NAME = LeapGui.FEATURE_PREFIX + "CYLINDRICAL";

  protected override Transformer ConstructTransformer(Transform root) {
    return new Transformer() {
      space = this,
      anchor = root,
      angleOffset = 0,
      heightOffset = 0,
      radiusOffset = radius,
      radiansPerMeter = 1.0f / radius
    };
  }

  protected override void SetTransformerRelativeTo(Transformer target, Transformer parent, Vector3 guiSpaceDelta) {
    target.angleOffset = parent.angleOffset + guiSpaceDelta.x / parent.radiusOffset;
    target.heightOffset = parent.heightOffset + guiSpaceDelta.y;
    target.radiusOffset = parent.radiusOffset + guiSpaceDelta.z;
    target.radiansPerMeter = 1.0f / (target.radiusOffset);
  }

  public class Transformer : IRadialTransformer {
    public LeapGuiCylindricalSpace space;
    public Transform anchor;

    public float angleOffset;
    public float heightOffset;
    public float radiusOffset;
    public float radiansPerMeter;

    public Vector3 TransformPoint(Vector3 localRectPos) {
      Vector3 anchorDelta;

      Vector3 anchorGuiPos = space.gui.transform.InverseTransformPoint(anchor.position);
      anchorDelta = localRectPos - anchorGuiPos;

      float angle = angleOffset + anchorDelta.x / radiusOffset;
      float height = heightOffset + anchorDelta.y;
      float radius = radiusOffset + anchorDelta.z;

      Vector3 position;
      position.x = Mathf.Sin(angle) * radius;
      position.y = height;
      position.z = Mathf.Cos(angle) * radius - space.radius;
      return position;
    }

    public Vector3 InverseTransformPoint(Vector3 localGuiPos) {
      localGuiPos.z += space.radius;

      float angle = Mathf.Atan2(localGuiPos.x, localGuiPos.z);
      float height = localGuiPos.y;
      float radius = new Vector2(localGuiPos.x, localGuiPos.z).magnitude;

      Vector3 anchorDelta;
      anchorDelta.x = (angle - angleOffset) * radiusOffset;
      anchorDelta.y = height - heightOffset;
      anchorDelta.z = radius - radiusOffset;

      Vector3 anchorGuiPos = space.gui.transform.InverseTransformPoint(anchor.position);
      Vector3 localRectPos = anchorGuiPos + anchorDelta;

      return localRectPos;
    }

    public Quaternion TransformRotation(Vector3 localRectPos, Quaternion localRectRot) {
      Vector3 anchorDelta;

      Vector3 anchorGuiPos = space.gui.transform.InverseTransformPoint(anchor.position);
      anchorDelta = localRectPos - anchorGuiPos;

      float angle = angleOffset + anchorDelta.x / radiusOffset;

      Quaternion rotation = Quaternion.Euler(0, angle * Mathf.Rad2Deg, 0);

      return rotation * localRectRot;
    }

    public Quaternion InverseTransformRotation(Vector3 localGuiPos, Quaternion localGuiRot) {
      localGuiPos.z += space.radius;

      float angle = Mathf.Atan2(localGuiPos.x, localGuiPos.z);

      return Quaternion.Euler(0, -angle * Mathf.Rad2Deg, 0) * localGuiRot;
    }

    public Vector3 TransformDirection(Vector3 localRectPos, Vector3 localRectDirection) {
      Vector3 anchorDelta;

      Vector3 anchorGuiPos = space.gui.transform.InverseTransformPoint(anchor.position);
      anchorDelta = localRectPos - anchorGuiPos;

      float angle = angleOffset + anchorDelta.x / radiusOffset;

      Quaternion rotation = Quaternion.Euler(0, angle * Mathf.Rad2Deg, 0);

      return rotation * localRectDirection;
    }

    public Vector3 InverseTransformDirection(Vector3 localGuiPos, Vector3 localGuiDirection) {
      localGuiPos.z += space.radius;

      float angle = Mathf.Atan2(localGuiPos.x, localGuiPos.z);

      return Quaternion.Euler(0, -angle * Mathf.Rad2Deg, 0) * localGuiDirection;
    }

    public Matrix4x4 GetTransformationMatrix(Vector3 localRectPos) {
      Vector3 anchorDelta;

      Vector3 anchorGuiPos = space.gui.transform.InverseTransformPoint(anchor.position);
      anchorDelta = localRectPos - anchorGuiPos;

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

    public Vector4 GetVectorRepresentation(LeapGuiElement element) {
      Vector3 elementGuiPos = space.gui.transform.InverseTransformPoint(element.transform.position);
      Vector3 anchorGuiPos = space.gui.transform.InverseTransformPoint(anchor.position);
      Vector3 delta = elementGuiPos - anchorGuiPos;

      Vector4 rep;
      rep.x = angleOffset + delta.x / radiusOffset;
      rep.y = heightOffset + delta.y;
      rep.z = radiusOffset + delta.z;
      rep.w = 1.0f / radiusOffset;
      return rep;
    }
  }
}
