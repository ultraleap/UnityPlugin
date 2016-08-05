using UnityEngine;
using System.Collections;

namespace Leap.Unity.RuntimeGizmos {

  /// <summary>
  /// This class controls the display of all the runtime gizmos
  /// that are either attatched to this gameObject, or a child of
  /// this gameObject.  Enable this component to allow the gizmos
  /// to be drawn, and disable it to hide them.
  /// </summary>
  public class RuntimeGizmoToggle : MonoBehaviour {
    public void OnEnable() { }
  }
}
