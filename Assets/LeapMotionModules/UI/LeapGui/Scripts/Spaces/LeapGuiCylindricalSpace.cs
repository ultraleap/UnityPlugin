using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity.Query;

public class LeapGuiCylindricalSpace : LeapGuiSpace {
  public const string FEATURE_NAME = LeapGui.FEATURE_PREFIX + "CYLINDRICAL";
  public const string RADIUS_PROPERTY = LeapGui.PROPERTY_PREFIX + "Cylindrical_Radius";

  public float radius = 1;

  private Dictionary<Transform, Vector3> _transformToPosition = new Dictionary<Transform, Vector3>();

  public Vector3 GetCylindricalPosition(Transform element) {
    return _transformToPosition[element];
  }

  public override void BuildElementData(Transform root) {
    _transformToPosition.Clear();
    _transformToPosition[root] = new Vector3(0, 0, radius);
    RefreshElementData(root);
  }

  public override void RefreshElementData(Transform root) {
    Vector3 rootPos = _transformToPosition[root];

    int childCount = root.childCount;
    for (int i = 0; i < childCount; i++) {
      Transform child = root.GetChild(i);

      Vector3 childPos = rootPos;
      childPos.x += child.transform.localPosition.x / rootPos.z;
      childPos.y += child.transform.localPosition.y;
      childPos.z += child.transform.localPosition.z;

      _transformToPosition[child] = childPos;

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
    Vector3 elementPos = _transformToPosition[element.transform];
    elementPos.x += localRectPos.x / elementPos.z;
    elementPos.y += localRectPos.y;
    elementPos.z += localRectPos.z;
    return elementPos;
  }

  public override Vector3 InverseTransformPoint(LeapGuiElement element, Vector3 worldGuiPos) {
    throw new System.NotImplementedException();
  }
}
