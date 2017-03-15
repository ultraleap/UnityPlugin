using System.Collections.Generic;

namespace Leap.Unity.Halfedge {

  public class Face {

    /// <summary> One of the halfedges bounding this face. </summary>
    public Halfedge halfedge;

    public VertsFromFaceEnumerator vertices {
      get { return new VertsFromFaceEnumerator(this); }
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

      public void Dispose() { }
    }

  }

}