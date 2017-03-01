using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Leap.Unity.Query;
using Leap.Unity.Attributes;

public static class LeapGuiMeshExtensions {
  public static LeapGuiMeshData Mesh(this LeapGuiElement element) {
    return element.data.Query().OfType<LeapGuiMeshData>().FirstOrDefault();
  }
}

[AddComponentMenu("")]
public class LeapGuiMeshData : LeapGuiElementData {

  [SerializeField]
  private Mesh _mesh;

  [Tooltip("All channels that are allowed to be remapped into atlas coordinates.")]
  [EnumFlags]
  [SerializeField]
  private UVChannelFlags _remappableChannels = UVChannelFlags.UV0 |
                                               UVChannelFlags.UV1 |
                                               UVChannelFlags.UV2 |
                                               UVChannelFlags.UV3;

  public Color tint = Color.white;

  public Mesh mesh { get; private set; }
  public UVChannelFlags remappableChannels { get; private set; }
  public bool isUsingProcedural { get; private set; }

  private static List<ProceduralMeshSource> _meshSourceList = new List<ProceduralMeshSource>();
  public void RefreshMeshData() {
    element.GetComponents(_meshSourceList);
    for (int i = 0; i < _meshSourceList.Count; i++) {
      var proceduralSource = _meshSourceList[i];
      Mesh proceduralMesh;
      UVChannelFlags proceduralFlags;
      if (proceduralSource.enabled && proceduralSource.TryGenerateMesh(this,
                                                                  out proceduralMesh,
                                                                  out proceduralFlags)) {
        mesh = proceduralMesh;
        remappableChannels = proceduralFlags;
        isUsingProcedural = true;
        return;
      }
    }

    mesh = _mesh;
    remappableChannels = _remappableChannels;
    isUsingProcedural = false;
  }
}
