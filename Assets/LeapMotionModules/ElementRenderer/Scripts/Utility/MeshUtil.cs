using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Leap.Unity.Query;

public static class MeshUtil {

  public static List<UVChannelFlags> allUvChannels;
  static MeshUtil() {
    allUvChannels = new List<UVChannelFlags>();
    allUvChannels.Add(UVChannelFlags.UV0);
    allUvChannels.Add(UVChannelFlags.UV1);
    allUvChannels.Add(UVChannelFlags.UV2);
    allUvChannels.Add(UVChannelFlags.UV3);
  }

  public static void RemapUvs(List<Vector4> uvs, Rect mapping) {
    for (int i = 0; i < uvs.Count; i++) {
      Vector4 uv = uvs[i];
      uv.x = mapping.x + uv.x * mapping.width;
      uv.y = mapping.y + uv.y * mapping.height;
      uvs[i] = uv;
    }
  }

  public static void GetUVsOrDefault(this Mesh mesh, int channel, List<Vector4> uvs) {
    mesh.GetUVs(channel, uvs);
    if (uvs.Count != mesh.vertexCount) {
      uvs.Fill(mesh.vertexCount, Vector4.zero);
    }
  }

  public static void SetUVsAuto(this Mesh mesh, int channel, List<Vector4> uvs) {
    if (uvs.Query().Any(v => v.w != 0)) {
      mesh.SetUVs(channel, uvs);
    } else if (uvs.Query().Any(v => v.z != 0)) {
      mesh.SetUVs(channel, uvs.Query().Select(v => (Vector3)v).ToList());
    } else {
      mesh.SetUVs(channel, uvs.Query().Select(v => (Vector2)v).ToList());
    }
  }

  public static int Index(this UVChannelFlags flags) {
    switch (flags) {
      case UVChannelFlags.UV0:
        return 0;
      case UVChannelFlags.UV1:
        return 1;
      case UVChannelFlags.UV2:
        return 2;
      case UVChannelFlags.UV3:
        return 3;
    }
    throw new InvalidOperationException();
  }
}
