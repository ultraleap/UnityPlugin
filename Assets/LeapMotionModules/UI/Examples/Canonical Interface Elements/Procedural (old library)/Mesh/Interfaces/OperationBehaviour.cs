using UnityEngine;

namespace Procedural.DynamicMesh {

  public interface IOperationBehaviour {
    IMeshOp meshOperation { get; }
  }

  public abstract class OperationBehaviour<T> : MonoBehaviour, IOperationBehaviour where T : IMeshOp {

    void Start() { }

    [SerializeField]
    protected T _operation;

    public IMeshOp meshOperation {
      get {
        return _operation;
      }
    }
  }
}
