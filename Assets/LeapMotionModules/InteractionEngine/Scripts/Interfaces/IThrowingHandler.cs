using UnityEngine;

namespace Leap.Unity.Interaction {

  public abstract class IThrowingHandler : IControllerBase {
    public abstract void OnThrow();
  }
}
