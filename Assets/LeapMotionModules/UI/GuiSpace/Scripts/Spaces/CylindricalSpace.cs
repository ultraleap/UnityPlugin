using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Leap.Unity.Attributes;

namespace Leap.Unity.Gui.Space {

  public class CylindricalSpace : GuiSpace {
    [SerializeField]
    private float _xOffset;

    [SerializeField]
    private float _zOffset;

    [SerializeField]
    private CylindricalType _type = CylindricalType.ConstantWidth;

    [Tooltip("When a gui element is this distance from the center of the space, it will have the same width " +
             "inside of the rect space and the gui space.")]
    [SerializeField]
    private float _offsetOfConstantWidth = 0.3f;

    public Vector3 localCenter {
      get {
        return new Vector3(_xOffset, 0, _zOffset);
      }
      set {
        _xOffset = value.x;
        _zOffset = value.z;
        //TODO: update guis
      }
    }

    public Vector3 worldCenter {
      get {
        return transform.TransformPoint(localCenter);
      }
      set {
        localCenter = transform.InverseTransformPoint(value);
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

    protected override string ShaderVariantName {
      get { throw new NotImplementedException(); }
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

    void OnDrawGizmosSelected() {
      Gizmos.matrix = transform.localToWorldMatrix * Matrix4x4.TRS(new Vector3(_xOffset, 0, _zOffset), Quaternion.identity, new Vector3(1, 0, 1));

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
