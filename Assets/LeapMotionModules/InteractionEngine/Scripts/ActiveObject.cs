using UnityEngine;

namespace Leap.Unity.Interaction {

  public class ActiveObject : MonoBehaviour {
    public IInteractionBehaviour interactionBehaviour;
    public int updateIndex = -1;

#if UNITY_EDITOR
    public void OnDrawGizmos() {

    }
#endif
  }
}
