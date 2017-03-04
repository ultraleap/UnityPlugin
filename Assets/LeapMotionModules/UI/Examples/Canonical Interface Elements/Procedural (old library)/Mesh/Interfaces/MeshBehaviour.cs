using UnityEngine;

namespace Procedural.DynamicMesh {

  public interface IMeshBehaviour {
    IMeshDef meshDefinition { get; }
  }

  public abstract class MeshBehaviour<T> : MonoBehaviour, IMeshBehaviour where T : IMeshDef {

    void Start() { }

    [SerializeField]
    protected T _meshDef;

    public IMeshDef meshDefinition {
      get {
        return _meshDef;
      }
    }
  }
}
