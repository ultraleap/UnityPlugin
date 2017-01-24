using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Leap.Unity.Attributes;

namespace Leap.Unity.Gui.Space {

  [ExecuteInEditMode]
  public class CylindricalSpace : GuiSpace {
    public const string SHADER_VARIANT_NAME = GuiMeshBaker.GUI_SPACE_SHADER_FEATURE_PREFIX + "CYLINDRICAL";
    public const string PARENT_POSITION_PROPERTY_NAME = "_GuiSpaceCylindrical_ParentPosition";
    public const string REFERENCE_RADIUS_PROPERTY_NAME = "_GuiSpaceCylindrical_ReferenceRadius";

    [SerializeField]
    private float _zOffset;

    [Disable]
    [SerializeField]
    private CylindricalType _type = CylindricalType.ConstantWidth;

    [Tooltip("When a gui element is this distance from the center of the space, it will have the same width " +
             "inside of the rect space and the gui space.")]
    [Disable]
    [SerializeField]
    private float _offsetOfConstantWidth = 0.3f;

    private List<Vector4> _elementPositions = new List<Vector4>();

    public float zOffset {
      get {
        return _zOffset;
      }
      set {
        _zOffset = value;
      }
    }

    public float RadiusOfConstantWidth {
      get {
        return _offsetOfConstantWidth;
      }
      set {
        _offsetOfConstantWidth = value;
      }
    }

    public override string ShaderVariantName {
      get {
        return SHADER_VARIANT_NAME;
      }
    }

    public override Vector3 FromRect(Vector3 rectPos) {
      float radius;

      if (_type == CylindricalType.ConstantWidth) {
        radius = rectPos.z;
      } else {
        radius = _offsetOfConstantWidth;
      }

      float theta = rectPos.x / radius;
      float dx = Mathf.Sin(theta) * rectPos.z;
      float dz = Mathf.Cos(theta) * rectPos.z;
      return new Vector3(dx, rectPos.y, dz);
    }

    public override Vector3 ToRect(Vector3 guiPos) {
      float z = Mathf.Sqrt(guiPos.x * guiPos.x + guiPos.z * guiPos.z);

      float radius;
      if (_type == CylindricalType.ConstantWidth) {
        radius = guiPos.z;
      } else {
        radius = _offsetOfConstantWidth;
      }

      float x = Mathf.Atan2(guiPos.x, guiPos.z) * radius;
      return new Vector3(x, guiPos.y, z);
    }

    public override void FromRect(Vector3 rectPos, Quaternion rectRot, out Vector3 guiPos, out Quaternion guiRot) {
      guiPos = FromRect(rectPos);
      guiRot = Quaternion.Euler(0, Mathf.Rad2Deg * Mathf.Atan2(guiPos.x, guiPos.z), 0) * rectRot;
    }

    public override void ToRect(Vector3 guiPos, Quaternion guiRot, out Vector3 rectPos, out Quaternion rectRot) {
      throw new System.NotImplementedException();
    }

    public override void BuildPerElementData() {
      _elementPositions.Clear();
      buildPerElementDataRecursively(transform, new Vector3(0, 0, _zOffset));
    }

    private void buildPerElementDataRecursively(Transform root, Vector3 rootPos) {
      int childCount = root.childCount;
      for (int i = 0; i < childCount; i++) {
        var child = root.GetChild(i);

        Vector3 delta = transform.InverseTransformPoint(child.position) - transform.InverseTransformPoint(root.position);

        Vector3 childPos = rootPos;
        childPos.x += delta.x / rootPos.z;
        childPos.y += delta.y;
        childPos.z += delta.z;

        LeapElement childElement = child.GetComponent<LeapElement>();
        if (childElement != null) {
          _elementPositions.Add(childPos);
        }

        buildPerElementDataRecursively(child, childPos);
      }
    }

    public override void RebuildPerElementData(int index, int count) {
      throw new NotImplementedException();
    }

    public override void UpdateMaterial(Material mat) {
      mat.SetVectorArray(PARENT_POSITION_PROPERTY_NAME, _elementPositions);
      mat.SetFloat(REFERENCE_RADIUS_PROPERTY_NAME, _zOffset);
    }

    void OnDrawGizmosSelected() {
      Gizmos.matrix = transform.localToWorldMatrix * Matrix4x4.TRS(new Vector3(0, 0, _zOffset), Quaternion.identity, new Vector3(1, 0, 1));

      if (_type == CylindricalType.Angular) {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(Vector3.zero, _offsetOfConstantWidth - _zOffset);
      }

      Gizmos.color = Color.white;
      Gizmos.DrawWireSphere(Vector3.zero, 0.1f);
    }

    public enum CylindricalType {
      ConstantWidth,
      Angular
    }
  }
}
