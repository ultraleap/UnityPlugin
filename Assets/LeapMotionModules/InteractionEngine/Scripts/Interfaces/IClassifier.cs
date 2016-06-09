using Leap.Unity.Interaction.CApi;

namespace Leap.Unity.Interaction {

  public abstract class IClassifier : IControllerBase {

    public abstract ManipulatorMode GetClassification(Hand hand);

  }
}
