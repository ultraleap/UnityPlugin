using UnityEngine;

namespace Leap.Unity.Interaction {

  public abstract class ISuspensionHandler : IControllerBase {
    public abstract float GetMaxSuspensionTime();
    public abstract void Suspend();
    public abstract void Resume();
    public abstract void Timeout();
  }
}
