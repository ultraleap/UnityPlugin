using UnityEngine;

namespace Leap.Unity.Interaction {

  public abstract class IHoldingController : IControllerBase {
    public abstract void AddHand(Hand hand);
    public abstract void RemoveHand(Hand hand);
    public abstract void GetHeldTransform(ReadonlyList<Hand> hands, out Vector3 position, out Quaternion rotation);
  }
}
