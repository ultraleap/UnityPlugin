using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FocusManager : MonoBehaviour {

  private static FocusManager s_instance = null;
  private static HashSet<Focusable> s_focusables = new HashSet<Focusable>();

  /// <summary> Returns true if the Focusable was successfully added; false if the Focusable was already tracked. </summary>
  public static bool Add(Focusable focusable) {
    return s_focusables.Add(focusable);
  }

  void Start() {
    if (s_instance != null) {
      Debug.LogError("Only one FocusManager is allowed in the scene.");
    }
    else {
      s_instance = this;
    }
  }

  void Update() {
    // Determine which object should have focus.

    // Do there need to be "groups" within which only one object can have focus, to allowed multiple focuses?

    // The manager here could/should also handle exclusivity.

    /* Possible strategies:
     * Rigidbodies + colliders: Leverage PhysX collider-qua-octtree optimization for broad-phase
     * Spin our own solution for broad-phase optimization?
     */
  }

}
