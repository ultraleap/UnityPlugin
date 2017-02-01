using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity.Query;

public static class LeapGuiMeshExtensions {
  public static LeapGuiMeshData Mesh(this LeapGuiElement element) {
    return element.data.Query().FirstOrDefault(d => d is LeapGuiMeshData) as LeapGuiMeshData;
  }
}

public class LeapGuiMeshData : LeapGuiElementData {

  [SerializeField]
  private Mesh _mesh;

  public Color color = Color.white;

  private static List<ProceduralMeshSource> _meshSourceList = new List<ProceduralMeshSource>();
  public Mesh mesh {
    get {
      element.GetComponents<ProceduralMeshSource>(_meshSourceList);
      for (int i = 0; i < _meshSourceList.Count; i++) {
        var proceduralSource = _meshSourceList[i];
        Mesh proceduralMesh;
        if (proceduralSource.enabled && proceduralSource.TryGenerateMesh(this, out proceduralMesh)) {
          return proceduralMesh;
        }
      }

      return _mesh;
    }
    set {
      _mesh = value;
    }
  }
}
