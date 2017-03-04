using UnityEngine;

namespace Procedural.DynamicPath {

  public abstract class PathBehaviourBase : MonoBehaviour {
    public abstract IPath Path {
      get;
    }
  }
}
