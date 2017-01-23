using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Gui.Space {

  public abstract class ProceduralMeshSource : MonoBehaviour {
    public abstract Mesh GetMesh();
  }
}
