using System;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("")]
[LeapGuiTag("None")]
public class LeapGuiRectSpace : LeapGuiSpace, ISupportsAddRemove {
  private Transformer _transformer = new Transformer();

  public void OnAddElement() { }
  public void OnRemoveElement() { }

  public override void BuildElementData(Transform root) { }
  public override void RefreshElementData(Transform root, int index, int count) { }

  public override ITransformer GetTransformer(Transform anchor) {
    return _transformer;
  }

  public class Transformer : ITransformer {

    public Vector3 TransformPoint(Vector3 localRectPos) {
      return localRectPos;
    }

    public Vector3 InverseTransformPoint(Vector3 localGuiPos) {
      return localGuiPos;
    }

    public Quaternion TransformRotation(Vector3 localRectPos, Quaternion localRectRot) {
      return localRectRot;
    }

    public Quaternion InverseTransformRotation(Vector3 localGuiPos, Quaternion localGuiRot) {
      return localGuiRot;
    }

    public Vector3 TransformDirection(Vector3 localRectPos, Vector3 localRectDirection) {
      return localRectDirection;
    }

    public Vector3 InverseTransformDirection(Vector3 localGuiPos, Vector3 localGuiDirection) {
      return localGuiDirection;
    }

    public Matrix4x4 GetTransformationMatrix(Vector3 localRectPos) {
      return Matrix4x4.TRS(localRectPos, Quaternion.identity, Vector3.one);
    }
  }
}
