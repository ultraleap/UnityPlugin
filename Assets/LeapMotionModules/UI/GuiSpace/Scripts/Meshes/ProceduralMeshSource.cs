using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Gui.Space {

  [RequireComponent(typeof(LeapElement))]
  public abstract class ProceduralMeshSource : MonoBehaviour {
    public abstract Mesh GetMesh();
    public abstract bool DoesMeshHaveAtlasUvs(int uvChannel);
  }
}
