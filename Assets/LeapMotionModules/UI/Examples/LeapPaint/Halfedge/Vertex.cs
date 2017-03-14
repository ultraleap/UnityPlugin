using UnityEngine;

namespace Leap.Unity.Halfedge {

  public class Vertex {

    public Vector3 position;

    public Vertex(float x, float y, float z) {
      this.position = new Vector3(x, y, z);
    }

    public Vertex(Vector3 position) {
      this.position = position;
    }

    public Halfedge halfedge;

    public static Vertex Copy(Vertex v) {
      return new Vertex(v.position);
    }

  }

}