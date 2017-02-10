using System;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("")]
[LeapGuiTag("Cylindrical")]
public class LeapGuiCylindricalSpace : LeapGuiRadialSpace, ISupportsAddRemove {
  public const string FEATURE_NAME = LeapGui.FEATURE_PREFIX + "CYLINDRICAL";

  private Dictionary<Transform, Transformer> _transformerData = new Dictionary<Transform, Transformer>();

  public void OnAddElements(List<LeapGuiElement> element, List<int> indexes) {
    BuildElementData(transform); //TODO, optimize
  }

  public void OnRemoveElements(List<int> toRemove) {
    BuildElementData(transform); //TODO, optimize
  }

  public override void BuildElementData(Transform root) {
    _transformerData.Clear();

    refreshElementData(root, new Transformer() {
      space = this,
      anchor = root,
      angleOffset = 0,
      heightOffset = 0,
      radiusOffset = radius,
      radiansPerMeter = 1.0f / radius
    });
  }

  public override void RefreshElementData(Transform root) {
    //TODO!
    //refreshElementData(root, root);
  }

  public override ITransformer GetAnchorTransformer(Transform anchor) {
    return _transformerData[anchor];
  }

  public override ITransformer GetLocalTransformer(LeapGuiElement element) {
    Vector3 elementGuiPos = gui.transform.InverseTransformPoint(element.transform.position);
    Vector3 anchorGuiPos = gui.transform.InverseTransformPoint(element.anchor.position);
    Vector3 delta = elementGuiPos - anchorGuiPos;

    Transformer anchor = _transformerData[element.anchor];

    return new Transformer() {
      angleOffset = anchor.angleOffset + delta.x / anchor.radiusOffset,
      heightOffset = anchor.heightOffset + delta.y,
      radiusOffset = anchor.radiusOffset + delta.z,
      radiansPerMeter = 1.0f / anchor.radiusOffset
    };
  }

  private void refreshElementData(Transform root, Transformer curr) {
    _transformerData[curr.anchor] = curr;

    var anchorComponent = root.GetComponent<AnchorOfConstantSize>();
    if (anchorComponent != null && anchorComponent.enabled) {
      Vector3 guiPosition = gui.transform.InverseTransformPoint(root.position);
      Vector3 guiAnchor = gui.transform.InverseTransformPoint(curr.anchor.position);
      Vector3 guiSpaceDelta = guiPosition - guiAnchor;

      Transformer newTransformer = new Transformer() {
        space = this,
        anchor = root,
        angleOffset = curr.angleOffset + guiSpaceDelta.x / curr.radiusOffset,
        heightOffset = curr.heightOffset + guiSpaceDelta.y,
        radiusOffset = curr.radiusOffset + guiSpaceDelta.z,
        radiansPerMeter = 1.0f / (curr.radiusOffset + guiSpaceDelta.z)
      };

      _transformerData[root] = newTransformer;
      curr = newTransformer;
    }

    int childCount = root.childCount;
    for (int i = 0; i < childCount; i++) {
      refreshElementData(root.GetChild(i), curr);
    }
  }

  public struct Transformer : ITransformer, IRadialTransformer {
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
      throw new NotImplementedException();
    }

    public Quaternion TransformRotation(Quaternion localRectRot) {
      throw new NotImplementedException();
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

    public Vector4 GetVectorRepresentation() {
      return new Vector4(angleOffset,
                         heightOffset,
                         radiusOffset,
                         radiansPerMeter);
    }
  }
}
