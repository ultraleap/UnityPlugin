using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("")]
[LeapGuiTag("None")]
public class LeapGuiRectSpace : LeapGuiSpace, ISupportsAddRemove {
  private Transformer _transformer = new Transformer();

  public void OnAddElements(List<LeapGuiElement> element, List<int> indexes) { }
  public void OnRemoveElements(List<int> toRemove) { }

  public override void BuildElementData(Transform root) { }
  public override void RefreshElementData(Transform root) { }

  public override ITransformer GetAnchorTransformer(Transform anchor) {
    return _transformer;
  }

  public override ITransformer GetLocalTransformer(LeapGuiElement element) {
    return _transformer;
  }

  public struct Transformer : ITransformer {
    public Vector3 InverseTransformDirection(Vector3 direction) {
      return direction;
    }

    public Vector3 InverseTransformPoint(Vector3 localGuiPos) {
      return localGuiPos;
    }

    public Quaternion InverseTransformRotation(Quaternion localGuiRot) {
      return localGuiRot;
    }

    public Vector3 TransformDirection(Vector3 direction) {
      return direction;
    }

    public Vector3 TransformPoint(Vector3 localRectPos) {
      return localRectPos;
    }

    public Quaternion TransformRotation(Quaternion localRectRot) {
      return localRectRot;
    }
  }
}
