using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Leap.Unity.Query;
using Leap.Unity.Attributes;

public static class LeapGuiMeshExtensions {
  public static LeapGuiMeshData Mesh(this LeapGuiElement element) {
    return element.data.Query().FirstOrDefault(d => d is LeapGuiMeshData) as LeapGuiMeshData;
  }
}

[AddComponentMenu("")]
public class LeapGuiMeshData : LeapGuiElementData {

  [SerializeField]
  private Mesh _customMesh;

  [Tooltip("All channels that are allowed to be remapped into atlas coordinates.")]
  [EnumFlags]
  [SerializeField]
  private UVChannelFlags _remappableChannels = UVChannelFlags.UV0 |
                                               UVChannelFlags.UV1 |
                                               UVChannelFlags.UV2 |
                                               UVChannelFlags.UV3;

  public Color tint = Color.white;

  private static List<ProceduralMeshSource> _meshSourceList = new List<ProceduralMeshSource>();
  public MeshData GetMeshData() {
    element.GetComponents(_meshSourceList);
    for (int i = 0; i < _meshSourceList.Count; i++) {
      var proceduralSource = _meshSourceList[i];
      MeshData proceduralData;
      if (proceduralSource.enabled && proceduralSource.TryGenerateMesh(this, out proceduralData)) {
        return proceduralData;
      }
    }

    return new MeshData() {
      mesh = _customMesh,
      remappableChannels = _remappableChannels
    };
  }

  private enum MeshType {
    Custom,
    Procedural
  }

  public struct MeshData {
    public Mesh mesh;
    public UVChannelFlags remappableChannels;
  }
}
