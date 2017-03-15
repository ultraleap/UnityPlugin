using System.Collections.Generic;

namespace Leap.Unity.Halfedge {

  public class Halfedge {

    /// <summary> The vertex this halfedge points to. (Right-hand rule around a face.) </summary>
    public Vertex vertex;

    /// <summary> The face this halfedge belongs to. </summary>
    public Face face;

    /// <summary> The next halfedge around the face. </summary>
    public Halfedge next;

    /// <summary> The halfedge opposite this one. Opposite halfedges belong to different faces. </summary>
    public Halfedge opposite;

    /// <summary> The previous halfedge around the face. </summary>
    public Halfedge prev;

    /// <summary> Returns an enumerator that traverses all of the faces in the halfedge mesh. </summary>
    public FaceEnumerator faces {
      get { return new FaceEnumerator(this); }
    }
    public class FaceEnumerator : IEnumerator<Face> {
      private Halfedge halfedgeStructure;
      private Face curFace;
      private bool needsFirstFace = true;

      private HashSet<Face> _facesVisitedCache = new HashSet<Face>();

      public FaceEnumerator GetEnumerator() {
        return this;
      }

      public FaceEnumerator(Halfedge halfedgeStructure) {
        this.halfedgeStructure = halfedgeStructure;
        curFace = halfedgeStructure.face;
        _facesVisitedCache.Clear();
      }

      private Face FindNewFace(Face face) {
        if (face == null) return null;

        _facesVisitedCache.Add(face);

        Halfedge curHalfedge = face.halfedge;
        Face newFace = null;
        do {
          Face testFace = curHalfedge.opposite.face;
          if (!_facesVisitedCache.Contains(testFace)) {
            return testFace;
          }
          curHalfedge = curHalfedge.next;
        } while (curHalfedge != face.halfedge);

        return newFace;
      }

      public Face Current {
        get { return curFace; }
      }

      object System.Collections.IEnumerator.Current {
        get { return Current; }
      }

      public bool MoveNext() {
        if (needsFirstFace) {
          needsFirstFace = false;
        }
        else {
          curFace = FindNewFace(curFace);
        }
        return curFace != null;
      }

      public void Reset() {
        curFace = halfedgeStructure.face;
        _facesVisitedCache.Clear();
      }

      public void Dispose() { }
    }

  }

}