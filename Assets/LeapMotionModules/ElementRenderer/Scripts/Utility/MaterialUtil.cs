using System.Collections.Generic;
using UnityEngine;

public static class MaterialUtil {

  public static void SetFloatArraySafe(this Material material, string property, List<float> list) {
    if (list.Count == 0) return;
    material.SetFloatArray(property, list);
  }

  public static void SetVectorArraySafe(this Material material, string property, List<Vector4> list) {
    if (list.Count == 0) return;
    material.SetVectorArray(property, list);
  }

  public static void SetMatrixArraySafe(this Material material, string property, List<Matrix4x4> list) {
    if (list.Count == 0) return;
    material.SetMatrixArray(property, list);
  }
}
