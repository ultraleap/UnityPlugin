using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MeshCache {

  public static void Clear() {
    _topologyCache.Clear();
    _colorCache.Clear();
  }

  private static Dictionary<Mesh, CachedTopology> _topologyCache = new Dictionary<Mesh, CachedTopology>();
  public static CachedTopology GetTopology(Mesh mesh) {
    CachedTopology topology;
    if (!_topologyCache.TryGetValue(mesh, out topology)) {
      topology.tris = mesh.GetIndices(0);
      topology.verts = mesh.vertices;
      _topologyCache[mesh] = topology;
    }
    return topology;
  }

  private static Dictionary<Mesh, Color[]> _colorCache = new Dictionary<Mesh, Color[]>();
  public static Color[] GetColors(Mesh mesh) {
    Color[] colors;
    if (!_colorCache.TryGetValue(mesh, out colors)) {
      colors = mesh.colors;
      if (colors.Length != mesh.vertexCount) {
        colors = null;
      }

      _colorCache[mesh] = colors;
    }
    return colors;
  }

  public struct CachedTopology {
    public Vector3[] verts;
    public int[] tris;
  }
}
