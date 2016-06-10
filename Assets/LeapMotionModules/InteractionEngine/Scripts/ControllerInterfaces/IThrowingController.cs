using UnityEngine;

namespace Leap.Unity.Interaction {

  public abstract class IThrowingController : IControllerBase {
    public abstract void OnHold(ReadonlyList<Hand> hands);
    public abstract void OnThrow(Hand throwingHand);
  }
}
