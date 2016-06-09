using UnityEngine;

namespace Leap.Unity.Interaction {

  public abstract class IThrowingController : IControllerBase {
    public abstract void OnHold();
    public abstract void OnThrow();
  }
}
