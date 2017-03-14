

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

    // /// <summary> The previous halfedge around the face. </summary>
    //public Halfedge prev;

  }

}