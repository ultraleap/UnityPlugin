using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Leap.Unity.Query;

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
        colors = new Color[mesh.vertexCount].Fill(Color.white);
      }

      _colorCache[mesh] = colors;
    }
    return colors;
  }

  private static Dictionary<UvKey, List<Vector4>> _uvCache = new Dictionary<UvKey, List<Vector4>>();
  public static List<Vector4> GetUvs(Mesh mesh, UVChannelFlags channel) {
    var key = new UvKey() { mesh = mesh, channel = channel };
    List<Vector4> uvs;
    if (!_uvCache.TryGetValue(key, out uvs)) {
      uvs = new List<Vector4>();
      mesh.GetUVs(channel.Index(), uvs);

      if (uvs.Count != mesh.vertexCount) {
        uvs.Fill(mesh.vertexCount, Vector4.zero);
      }

      _uvCache[key] = uvs;
    }
    return uvs;
  }

  public struct CachedTopology {
    public Vector3[] verts;
    public int[] tris;
  }

  private struct UvKey {
    public Mesh mesh;
    public UVChannelFlags channel;
  }
}
