using UnityEngine;

namespace Procedural.DynamicMesh {

  public interface IModifierBehaviour {
    IMeshMod meshModifier { get; }
  }

  public abstract class ModifierBehaviour<T> : MonoBehaviour, IModifierBehaviour where T : IMeshMod {

    void Start() { }

    [SerializeField]
    protected T _modifier;

    public IMeshMod meshModifier {
      get {
        return _modifier;
      }
    }
  }
}
