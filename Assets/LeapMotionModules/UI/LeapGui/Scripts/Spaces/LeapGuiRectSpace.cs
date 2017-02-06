using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("")]
[LeapGuiTag("None")]
public class LeapGuiRectSpace : LeapGuiSpace, ISupportsAddRemove {

  public void OnAddElements(List<LeapGuiElement> element, List<int> indexes) { }
  public void OnRemoveElements(List<int> toRemove) { }

  public override void BuildElementData(Transform root) { }
  public override void RefreshElementData(Transform root) { }

  public override Vector3 InverseTransformPoint(Vector3 worldGuiPos) {
    return worldGuiPos;
  }

  public override Vector3 InverseTransformPoint(LeapGuiElement element, Vector3 worldGuiPos) {
    return worldGuiPos;
  }

  public override Vector3 TransformPoint(Vector3 worldRectPos) {
    return worldRectPos;
  }

  public override Vector3 TransformPoint(LeapGuiElement element, Vector3 localRectPos) {
    return localRectPos;
  }
}
