/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Leap.Unity.Query;

namespace Leap.Unity.GraphicalRenderer {

  public static class MeshCache {

    public static void Clear() {
      _topologyCache.Clear();
      _normalCache.Clear();
      _colorCache.Clear();
      _uvCache.Clear();
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

    private static Dictionary<Mesh, Vector3[]> _normalCache = new Dictionary<Mesh, Vector3[]>();
    public static Vector3[] GetNormals(Mesh mesh) {
      Vector3[] normals;
      if (!_normalCache.TryGetValue(mesh, out normals)) {
        normals = mesh.normals;
        if (normals.Length != mesh.vertexCount) {
          mesh.RecalculateNormals();
          normals = mesh.normals;
        }

        _normalCache[mesh] = normals;
      }
      return normals;
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
      var key = new UvKey() { mesh = mesh, channel = (int)channel };
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

    private struct UvKey : IComparable<UvKey>, IEquatable<UvKey> {
      public Mesh mesh;
      public int channel;

      public int CompareTo(UvKey other) {
        if (other.mesh != mesh) return 1;
        if (other.channel != channel) return 1;
        return 0;
      }

      public override int GetHashCode() {
        return mesh.GetHashCode() + channel;
      }

      public bool Equals(UvKey other) {
        return other.mesh == mesh && other.channel == channel;
      }
    }
  }
}
