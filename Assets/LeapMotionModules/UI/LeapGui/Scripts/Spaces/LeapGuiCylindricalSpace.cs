using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity.Query;

public class LeapGuiCylindricalSpace : LeapGuiSpace {
  public const string FEATURE_NAME = LeapGui.FEATURE_PREFIX + "CYLINDRICAL";
  public const string RADIUS_PROPERTY = LeapGui.PROPERTY_PREFIX + "Cylindrical_Radius";

  public float radius = 1;

  private Dictionary<Transform, ElementData> _transformToAnchor = new Dictionary<Transform, ElementData>();

  public Vector3 GetAnchorPosition(Transform element) {
    return _transformToAnchor[element];
  }

  public override void BuildElementData(Transform root) {
    _transformToAnchor.Clear();
    _transformToAnchor[root] = new Vector3(0, 0, radius);
    RefreshElementData(root);
  }

  public override void RefreshElementData(Transform root) {
    ElementData anchor = _transformToAnchor[root];
    Vector3 anchorPos = anchor.cylindricalPosition;

    var anchorComponent = root.GetComponent<ConstantSizeAnchor>();
    if (anchorComponent != null) {
      Vector3 delta = root.position - anchor.transform.position;
      anchorPos.x += delta.x / anchorPos.z;
      anchorPos.y += delta.y;
      anchorPos.z += delta.z;
    }

    int childCount = root.childCount;
    for (int i = 0; i < childCount; i++) {
      Transform child = root.GetChild(i);
      Vector3 delta = child.position - Vector3.zero;

      Vector3 childPos;
      childPos.x = delta.x / radius;
      childPos.y = delta.y; 
      childPos.z = delta.z + radius;

      _transformToAnchor[child] = childPos;

      RefreshElementData(child);
    }
  }

  public override Vector3 TransformPoint(Vector3 worldRectPos) {
    throw new System.NotImplementedException();
  }

  public override Vector3 InverseTransformPoint(Vector3 worldGuiPos) {
    throw new System.NotImplementedException();
  }

  public override Vector3 TransformPoint(LeapGuiElement element, Vector3 localRectPos) {
    Vector3 elementPos = _transformToAnchor[element.transform];
    elementPos.x += localRectPos.x / elementPos.z;
    elementPos.y += localRectPos.y;
    elementPos.z += localRectPos.z;
    return elementPos;
  }

  public override Vector3 InverseTransformPoint(LeapGuiElement element, Vector3 worldGuiPos) {
    throw new System.NotImplementedException();
  }

  private struct ElementData {
    public Transform anchor;
    public Vector3 anchorPosition;
    public Vector3 position;
  }
}
