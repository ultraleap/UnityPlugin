using System;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("")]
[LeapGuiTag("None")]
public class LeapGuiRectSpace : LeapGuiSpace, ISupportsAddRemove {
  private Transformer _transformer = new Transformer();

  public void OnAddElements(List<LeapGuiElement> element, List<int> indexes) { }
  public void OnRemoveElements(List<int> toRemove) { }

  public override void BuildElementData(Transform root) { }
  public override void RefreshElementData(Transform root, int index, int count) { }

  public override ITransformer GetTransformer(Transform anchor) {
    return _transformer;
  }

  public class Transformer : ITransformer {

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

    public Matrix4x4 GetTransformationMatrix(Vector3 localRectPos) {
      return Matrix4x4.identity;
    }
  }
}
