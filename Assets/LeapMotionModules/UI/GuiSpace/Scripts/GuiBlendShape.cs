using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GuiBlendShape : MonoBehaviour {
  [SerializeField]
  private Mesh mesh;

  public Vector3[] vertices {
    get {
      return mesh.vertices;
    }
  }
}
