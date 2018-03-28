/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.GraphicalRenderer {

  public static class MaterialUtil {

    public static void SetFloatArraySafe(this Material material, string property, List<float> list) {
      if (list.Count == 0) return;
      material.SetFloatArray(property, list);
    }

    public static void SetVectorArraySafe(this Material material, string property, List<Vector4> list) {
      if (list.Count == 0) return;
      material.SetVectorArray(property, list);
    }

    public static void SetColorArraySafe(this Material material, string property, List<Color> list) {
      if (list.Count == 0) return;
      material.SetColorArray(property, list);
    }

    public static void SetMatrixArraySafe(this Material material, string property, List<Matrix4x4> list) {
      if (list.Count == 0) return;
      material.SetMatrixArray(property, list);
    }
  }
}
