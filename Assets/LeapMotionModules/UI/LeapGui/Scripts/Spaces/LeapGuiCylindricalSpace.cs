using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity.Query;

public class LeapGuiCylindricalSpace : LeapGuiSpace {
  public const string FEATURE_NAME = LeapGui.FEATURE_PREFIX + "CYLINDRICAL";
  public const string RADIUS_PROPERTY = LeapGui.PROPERTY_PREFIX + "Cylindrical_Radius";

  public float radius = 1;

  private Dictionary<AnchorOfConstantSize, AnchorData> _anchorData = new Dictionary<AnchorOfConstantSize, AnchorData>();


  public AnchorData GetAnchorData(AnchorOfConstantSize anchor) {
    return _anchorData[anchor];
  }

  public ElementParameters GetElementParameters(AnchorOfConstantSize anchor, Vector3 rectPosWorld) {
    AnchorData anchorData;
    Vector3 anchorDelta;

    Vector3 guiPos = gui.transform.InverseTransformPoint(rectPosWorld);
    if (anchor == null) {
      anchorDelta = guiPos;
      anchorData.anchorAngle = 0;
      anchorData.anchorHeight = 0;
      anchorData.anchorRadius = radius;
    } else {
      Vector3 anchorGuiPos = gui.transform.InverseTransformPoint(anchor.transform.position);
      anchorDelta = guiPos - anchorGuiPos;
      anchorData = _anchorData[anchor];
    }

    float angleOffset = anchorData.anchorAngle + anchorDelta.x / anchorData.anchorRadius;
    float heightOffset = anchorData.anchorHeight + anchorDelta.y;
    float radiusOffset = anchorData.anchorRadius + anchorDelta.z;

    ElementParameters parameters;
    parameters.angleOffset = angleOffset;
    parameters.radianPerMeter = 1.0f / anchorData.anchorRadius;
    parameters.heightOffset = heightOffset;
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
        parentData.anchorAngle = 0;
        parentData.anchorHeight = 0;
        parentData.anchorRadius = radius;
      }

      AnchorData newData = parentData;
      newData.anchorAngle += guiSpaceDelta.x / parentData.anchorRadius;
      newData.anchorHeight += guiSpaceDelta.y;
      newData.anchorRadius += guiSpaceDelta.z;

      parentAnchor = anchorComponent;
      _anchorData[parentAnchor] = newData;
    }

    int childCount = root.childCount;
    for (int i = 0; i < childCount; i++) {
      refreshElementData(root.GetChild(i), parentAnchor);
    }
  }

  public override Vector3 TransformPoint(Vector3 worldRectPos) {
    throw new System.NotImplementedException();
  }

  public override Vector3 InverseTransformPoint(Vector3 worldGuiPos) {
    throw new System.NotImplementedException();
  }

  public override Vector3 TransformPoint(LeapGuiElement element, Vector3 localRectPos) {
    //Vector3 elementPos = _transformToAnchor[element.transform];
    //elementPos.x += localRectPos.x / elementPos.z;
    //elementPos.y += localRectPos.y;
    //elementPos.z += localRectPos.z;
    //return elementPos;
    return Vector3.zero;
  }

  public override Vector3 InverseTransformPoint(LeapGuiElement element, Vector3 worldGuiPos) {
    throw new System.NotImplementedException();
  }

  public struct ElementParameters {
    public float angleOffset;
    public float radianPerMeter;
    public float heightOffset;
    public float radiusOffset;

    public static implicit operator Vector4(ElementParameters parameters) {
      return new Vector4(parameters.angleOffset,
                         parameters.radianPerMeter,
                         parameters.heightOffset,
                         parameters.radiusOffset);
    }
  }

  public struct AnchorData {
    public float anchorAngle;
    public float anchorHeight;
    public float anchorRadius;
  }
}
