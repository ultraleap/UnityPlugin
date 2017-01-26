using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class LeapGuiMeshFeature : LeapGuiFeature<LeapGuiMeshData> {
  public bool uv0;
  public bool uv1;
  public bool uv2;
  public bool uv3;
  public bool color;
  public Color tint;
  public bool normals;

  public IEnumerable<UVChannelFlags> enabledUvChannels {
    get {
      if (uv0) yield return UVChannelFlags.UV0;
      if (uv1) yield return UVChannelFlags.UV1;
      if (uv2) yield return UVChannelFlags.UV2;
      if (uv3) yield return UVChannelFlags.UV3;
    }
  }
}

public class LeapGuiMeshData : LeapGuiElementData {
  public Mesh mesh;
  public Color color;
}