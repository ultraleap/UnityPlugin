using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Leap.Unity.Attributes;

/// <summary>
/// Just a copy of the UnityEditor.UpwrapParam struct, which is
/// not serializable.
/// </summary>
[Serializable]
public class LightmapUnwrapSettings {

  [EditTimeOnly]
  [Range(0, 1)]
  public float angleError = 0.08f;

  [EditTimeOnly]
  [Range(0, 1)]
  public float areaError = 0.15f;

  [EditTimeOnly]
  [Range(0, 180)]
  public float hardAngle = 88;

  [EditTimeOnly]
  [MinValue(0)]
  public float packMargin = 0.00390625f;

#if UNITY_EDITOR
  public void GenerateLightmapUvs(Mesh mesh) {
    UnwrapParam param = new UnwrapParam();
    param.angleError = angleError;
    param.areaError = areaError;
    param.hardAngle = hardAngle;
    param.packMargin = packMargin;

    Unwrapping.GenerateSecondaryUVSet(mesh, param);
  }
#endif
}
