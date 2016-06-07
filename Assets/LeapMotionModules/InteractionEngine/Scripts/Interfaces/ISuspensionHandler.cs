using UnityEngine;

namespace Leap.Unity.Interaction {

  public abstract class ISuspensionHandler : IHandlerBase {
    public abstract void BeginSuspension();
    public abstract void EndSuspension();
  }
}
