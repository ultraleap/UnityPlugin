using Leap.Unity.Interaction.CApi;

namespace Leap.Unity.Interaction {

  public abstract class IGraspController : IControllerBase {

    public abstract ManipulatorMode GetGraspMode(Hand hand);

  }
}
