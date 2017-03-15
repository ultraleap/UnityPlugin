using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Halfedge {

  public enum PrimitiveType {
    Tetrahedron
  }

  public static class Ops {

    #region Traversal

    private static List<Vertex> s_vertsCache = new List<Vertex>();
    /// <summary> Returns a temporary list of all "common" vertices to the argument
    /// halfedge's vertex, including that vertex. "Common" vertices occupy the same
    /// position in space and share edges but belong to different faces. </summary>
    public static List<Vertex> GetCommonVertices(Halfedge halfedge) {
      s_vertsCache.Clear();

      var h = halfedge;
      do {
        s_vertsCache.Add(h.vertex);
        h = halfedge.opposite.prev;
      } while (h != halfedge);
      return s_vertsCache;
    }

    private static HashSet<Halfedge> s_visitedHalfedgeCache = new HashSet<Halfedge>();
    private static List<Vertex> s_commonVertsForHalfedgeCache = new List<Vertex>();
    public static void TraverseCommonVertices(Halfedge halfedge, Action<List<Vertex>> OnUniqueCommonVertex) {
      s_visitedHalfedgeCache.Clear();
      
      Halfedge curHalfedge = halfedge;
      do {
        s_commonVertsForHalfedgeCache.Clear();
        var h = curHalfedge;
        do { // Loop around vertex pivot
          s_commonVertsForHalfedgeCache.Add(h.vertex);
          s_visitedHalfedgeCache.Add(h);
          h = h.opposite.prev;
        } while (h != curHalfedge);
        OnUniqueCommonVertex(s_commonVertsForHalfedgeCache);

        curHalfedge = FindNewHalfedgePivot(curHalfedge, s_visitedHalfedgeCache);

      } while (curHalfedge != null);
    }

    private static Halfedge FindNewHalfedgePivot(Halfedge h, HashSet<Halfedge> visitedHalfedges) {
      Halfedge h0 = h;
      do {
        if (!visitedHalfedges.Contains(h.opposite)) {
          return h.opposite;
        }
        h = h.next;
      } while (h0 != h);
      return null;
    }

    #endregion

    #region Face Manipulation

    /// <summary> "Pokes" a face, replacing an N-gon face with N triangular faces
    /// that meet at a new Vertex at the center of the original face. </summary>
    public static Vertex Poke(this Face face) {
      return null;
    }

    #endregion

  }

}