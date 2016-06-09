using Leap.Unity.Interaction.CApi;

namespace Leap.Unity.Interaction {

  public class GraspControllerDefault : IGraspController {

    public override ManipulatorMode GetGraspMode(Hand hand) {
      var scene = _obj.Manager.Scene;
      INTERACTION_HAND_RESULT result;
      InteractionC.GetHandResult(ref scene, (uint)hand.Id, out result);
      return result.classification;
    }

  }
}
