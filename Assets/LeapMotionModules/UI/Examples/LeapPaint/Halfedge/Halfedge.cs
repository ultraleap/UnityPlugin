using System;
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

    [ThreadStatic]
    private static FaceEnumerator _faceEnum;
    /// <summary> Gets an enumerator that will traverse all of the faces in the halfedge mesh.
    /// As an optimization, only one backing Enumerator is constructed (per accessing thread).
    /// If the getter is called twice on one thread, the second call will return the same object
    /// as the first, but the object will also be reset back to its original state.
    /// 
    /// In other words, foreach will just work, but call this once and store a reference if you
    /// want to do fancy manual MoveNext stuff. </summary>
    public FaceEnumerator faces {
      get {
        if (_faceEnum == null) {
          _faceEnum = new FaceEnumerator(this);
        }
        else {
          _faceEnum.ResetWithNewHalfedge(this);
        }
        return _faceEnum;
      }
    }
    public class FaceEnumerator : IEnumerator<Face> {
      private Halfedge halfedge;
      private Face curFace;
      private bool needsFirstFace = true;

      private HashSet<Face> _facesVisitedCache = new HashSet<Face>();

      public FaceEnumerator GetEnumerator() {
        return this;
      }

      public FaceEnumerator(Halfedge halfedgeStructure) {
        this.halfedge = halfedgeStructure;
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
        curFace = halfedge.face;
        _facesVisitedCache.Clear();
      }

      public void ResetWithNewHalfedge(Halfedge halfedge) {
        this.halfedge = halfedge;
        Reset();
      }

      public void Dispose() { }
    }

  }

}