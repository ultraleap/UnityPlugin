using System;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("")]
[LeapGuiTag("Spherical")]
public class LeapGuiSphericalSpace : LeapGuiRadialSpace, ISupportsAddRemove {
  public const string FEATURE_NAME = LeapGui.FEATURE_PREFIX + "SPHERICAL";

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
      angleXOffset = 0,
      angleYOffset = 0,
      radiusOffset = radius,
      radiansPerMeter = 1.0f / radius
    });
  }

  public override void RefreshElementData(Transform root) {
    //TODO!
    //refreshElementData(root, root);
  }

  public override ITransformer GetTransformer(Transform anchor) {
    return _transformerData[anchor];
  }

  /*
  public override ITransformer GetLocalTransformer(LeapGuiElement element) {
    Vector3 elementGuiPos = gui.transform.InverseTransformPoint(element.transform.position);
    Vector3 anchorGuiPos = gui.transform.InverseTransformPoint(element.anchor.position);
    Vector3 delta = elementGuiPos - anchorGuiPos;

    Transformer anchor = _transformerData[element.anchor];

    return new Transformer() {
      angleXOffset = anchor.angleXOffset + delta.x / anchor.radiusOffset,
      angleYOffset = anchor.angleYOffset + delta.y / anchor.radiusOffset,
      radiusOffset = anchor.radiusOffset + delta.z,
      radiansPerMeter = 1.0f / anchor.radiusOffset
    };
  }
  */

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
        angleXOffset = curr.angleXOffset + guiSpaceDelta.x / curr.radiusOffset,
        angleYOffset = curr.angleYOffset + guiSpaceDelta.y / curr.radiusOffset,
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
