
namespace Leap.Unity.Interaction {

  public abstract class ISuspensionController : IControllerBase {
    public abstract float MaxSuspensionTime { get; }
    public abstract void Suspend();
    public abstract void Resume();
    public abstract void Timeout();
  }
}
