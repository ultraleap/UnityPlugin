using System;
using System.Collections.Generic;

namespace Leap.Unity.Halfedge {

  public class Face {

    /// <summary> One of the halfedges bounding this face. </summary>
    public Halfedge halfedge;

    //public string name; // Useful for debugging.

    [ThreadStatic]
    private static HalfedgesAroundFaceEnumerator _halfedgeEnum;
    /// <summary> Gets an enumerator that returns every halfedge bounding this face. </summary>
    public HalfedgesAroundFaceEnumerator halfedges {
      get {
        if (_halfedgeEnum == null) _halfedgeEnum = new HalfedgesAroundFaceEnumerator(this);
        _halfedgeEnum.ResetForNewFace(this);
        return _halfedgeEnum;
      }
    }
    public class HalfedgesAroundFaceEnumerator : IEnumerator<Halfedge> {
      private Face face;
      private Halfedge h0;
      private Halfedge h;

      public HalfedgesAroundFaceEnumerator(Face face) { this.face = face; }
      public HalfedgesAroundFaceEnumerator GetEnumerator() { return this; }

      public bool MoveNext() {
        if (h0 == null) {
          h0 = h = face.halfedge;
          return h != null;
        }
        else {
          h = h.next;
          return h != h0;
        }
      }

      public Halfedge Current { get { return h; } }
      object System.Collections.IEnumerator.Current { get { return Current; } }
      public void Reset() { h = h0 = null; }
      public void ResetForNewFace(Face face) { this.face = face; Reset(); }
      public void Dispose() { }
    }

    [ThreadStatic]
    private static VertsFromFaceEnumerator _vertEnum;
    /// <summary> Gets an enumerator that returns every vertex in this face. </summary>
    public VertsFromFaceEnumerator vertices {
      get {
        if (_vertEnum == null) _vertEnum = new VertsFromFaceEnumerator(this);
        _vertEnum.ResetForNewFace(this);
        return _vertEnum;
      }
    }
    public class VertsFromFaceEnumerator : IEnumerator<Vertex> {
      private Face face;
      private Halfedge h0;
      private Halfedge h;

      public VertsFromFaceEnumerator(Face face) { this.face = face; }
      public VertsFromFaceEnumerator GetEnumerator() { return this; }

      public bool MoveNext() {
        if (h0 == null) {
          h0 = h = face.halfedge;
          return h != null;
        }
        else {
          h = h.next;
          return h != h0;
        }
      }

      public Vertex Current { get { return h.vertex; } }
      object System.Collections.IEnumerator.Current { get { return Current; } }
      public void Reset() { h = h0 = null; }
      public void ResetForNewFace(Face face) { this.face = face; Reset();  }
      public void Dispose() { }
    }

  }

}