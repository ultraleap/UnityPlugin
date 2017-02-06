using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("")]
[LeapGuiTag("Spherical")]
public class LeapGuiSphericalSpace : LeapGuiSpace, ISupportsAddRemove {
  public const string FEATURE_NAME = LeapGui.FEATURE_PREFIX + "SPHERICAL";
  public const string RADIUS_PROPERTY = LeapGui.PROPERTY_PREFIX + "Spherical_Radius";

  public float radius = 1;

  private Dictionary<AnchorOfConstantSize, AnchorData> _anchorData = new Dictionary<AnchorOfConstantSize, AnchorData>();

  public void OnAddElements(List<LeapGuiElement> element, List<int> indexes) {
    BuildElementData(transform); //TODO, optimize
  }

  public void OnRemoveElements(List<int> toRemove) {
    BuildElementData(transform); //TODO, optimize
  }

  public AnchorData GetAnchorData(AnchorOfConstantSize anchor) {
    return _anchorData[anchor];
  }

  public ElementParameters GetElementParameters(AnchorOfConstantSize anchor, Vector3 rectPosWorld) {
    AnchorData anchorData;
    Vector3 anchorDelta;

    Vector3 guiPos = gui.transform.InverseTransformPoint(rectPosWorld);
    if (anchor == null) {
      anchorDelta = guiPos;
      anchorData.anchorAngleX = 0;
      anchorData.anchorAngleY = 0;
      anchorData.anchorRadius = radius;
    } else {
      Vector3 anchorGuiPos = gui.transform.InverseTransformPoint(anchor.transform.position);
      anchorDelta = guiPos - anchorGuiPos;
      anchorData = _anchorData[anchor];
    }

    float angleXOffset = anchorData.anchorAngleX + anchorDelta.x / anchorData.anchorRadius;
    float angleYOffset = anchorData.anchorAngleY + anchorDelta.y / anchorData.anchorRadius;
    float radiusOffset = anchorData.anchorRadius + anchorDelta.z;

    ElementParameters parameters;
    parameters.angleXOffset = angleXOffset;
    parameters.angleYOffset = angleYOffset;
    parameters.radianPerMeter = 1.0f / anchorData.anchorRadius;
    parameters.radiusOffset = radiusOffset;

    return parameters;
  }

  public override void BuildElementData(Transform root) {
    _anchorData.Clear();
    refreshElementData(root, null);
  }

  public override void RefreshElementData(Transform root) {
    refreshElementData(root, null);
  }

  private void refreshElementData(Transform root, AnchorOfConstantSize parentAnchor) {
    var anchorComponent = root.GetComponent<AnchorOfConstantSize>();
    if (anchorComponent != null && anchorComponent.enabled) {
      Vector3 guiSpaceDelta;
      AnchorData parentData;

      Vector3 guiPosition = gui.transform.InverseTransformPoint(root.position);
      if (parentAnchor != null) {
        Vector3 guiAnchor = gui.transform.InverseTransformPoint(parentAnchor.transform.position);
        guiSpaceDelta = guiPosition - guiAnchor;
        parentData = _anchorData[parentAnchor];
      } else {
        guiSpaceDelta = guiPosition;
        parentData.anchorAngleX = 0;
        parentData.anchorAngleY = 0;
        parentData.anchorRadius = radius;
      }

      AnchorData newData = parentData;
      newData.anchorAngleX += guiSpaceDelta.x / parentData.anchorRadius;
      newData.anchorAngleY += guiSpaceDelta.y / parentData.anchorRadius;
      newData.anchorRadius += guiSpaceDelta.z;

      parentAnchor = anchorComponent;
      _anchorData[parentAnchor] = newData;
    }

    int childCount = root.childCount;
    for (int i = 0; i < childCount; i++) {
      refreshElementData(root.GetChild(i), parentAnchor);
    }
  }

  public override Vector3 TransformPoint(Vector3 localRectPos) {
    Vector3 localGuiPos;
    float angle = localRectPos.x / radius;
    float pointRadius = localRectPos.z + radius;
    localGuiPos.x = Mathf.Sin(angle) * pointRadius;
    localGuiPos.y = localRectPos.y;
    localGuiPos.z = Mathf.Cos(angle) * pointRadius - radius;
    return localGuiPos;
  }

  public override Vector3 InverseTransformPoint(Vector3 localGuiPos) {
    throw new System.NotImplementedException();
  }

  public override Vector3 TransformPoint(LeapGuiElement element, Vector3 localRectPos) {
    AnchorData anchorData;
    Vector3 anchorDelta;

    if (element.anchor == null) {
      anchorDelta = localRectPos;
      anchorData.anchorAngleX = 0;
      anchorData.anchorAngleY = 0;
      anchorData.anchorRadius = radius;
    } else {
      Vector3 anchorGuiPos = gui.transform.InverseTransformPoint(element.anchor.transform.position);
      anchorDelta = localRectPos - anchorGuiPos;
      anchorData = _anchorData[element.anchor];
    }

    float angleXOffset = anchorData.anchorAngleX + anchorDelta.x / anchorData.anchorRadius;
    float angleYOffset = anchorData.anchorAngleY + anchorDelta.y / anchorData.anchorRadius;
    float radiusOffset = anchorData.anchorRadius + anchorDelta.z;

    Vector3 position;
    position.x = 0;
    position.y = Mathf.Sin(angleYOffset) * radiusOffset;
    position.z = Mathf.Cos(angleYOffset) * radiusOffset;

    Vector3 position2;
    position2.x = Mathf.Sin(angleXOffset) * position.z;
    position2.y = position.y;
    position2.z = Mathf.Cos(angleXOffset) * position.z;

    position2.z -= radius;

    return position2;
  }

  public override Vector3 InverseTransformPoint(LeapGuiElement element, Vector3 worldGuiPos) {
    throw new System.NotImplementedException();
  }

  public struct ElementParameters {
    public float angleXOffset;
    public float angleYOffset;
    public float radianPerMeter;
    public float radiusOffset;

    public static implicit operator Vector4(ElementParameters parameters) {
      return new Vector4(parameters.angleXOffset,
                         parameters.angleYOffset,
                         parameters.radianPerMeter,
                         parameters.radiusOffset);
    }
  }

  public struct AnchorData {
    public float anchorAngleX; //rotation around the **Y** axis
    public float anchorAngleY; //rotation around the **X** axis
    public float anchorRadius;
  }

  private const float GIZMO_WIDTH = 1.0f;
  private const float GIZMO_HEIGHT = 0.2f;
  private const int GIZMO_Y_COUNT = 2;
  private const float GIZMO_RES = 0.05f;
  void OnDrawGizmos() {
    Gizmos.matrix = gui.transform.localToWorldMatrix;
    Gizmos.color = Color.white;
    for (int y = -GIZMO_Y_COUNT; y <= GIZMO_Y_COUNT; y++) {
      float dy = y * GIZMO_HEIGHT / GIZMO_Y_COUNT;
      for (float dx = 0; dx < 1; dx += GIZMO_RES) {
        Vector3 a = new Vector3(dx, dy, 0);
        Vector3 b = new Vector3(dx + GIZMO_RES, dy, 0);
        Vector3 c = a;
        Vector3 d = b;
        c.x = -c.x;
        d.x = -d.x;

        a = TransformPoint(a);
        b = TransformPoint(b);
        c = TransformPoint(c);
        d = TransformPoint(d);

        Gizmos.DrawLine(a, b);
        Gizmos.DrawLine(c, d);
      }
    }
  }
}
