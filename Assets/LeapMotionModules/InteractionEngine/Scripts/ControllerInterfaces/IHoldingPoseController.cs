using UnityEngine;

namespace Leap.Unity.Interaction {

  public abstract class IHoldingPoseController : IControllerBase {
    public abstract void AddHand(Hand hand);
    public abstract void TransferHandId(int oldId, int newId);
    public abstract void RemoveHand(Hand hand);
    public abstract void GetHoldingPose(ReadonlyList<Hand> hands, out Vector3 position, out Quaternion rotation);
  }
}
