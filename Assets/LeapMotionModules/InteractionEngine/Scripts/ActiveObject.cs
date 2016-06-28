using UnityEngine;

namespace Leap.Unity.Interaction {

  public class ActiveObject : MonoBehaviour {
    public IInteractionBehaviour interactionBehaviour;
    public int updateIndex = -1;
  }
}
