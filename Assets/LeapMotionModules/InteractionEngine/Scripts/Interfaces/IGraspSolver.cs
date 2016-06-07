using UnityEngine;

namespace Leap.Unity.Interaction {

  public abstract class IGraspSolver : IHandlerBase {
    public abstract void AddHand(Hand hand);
    public abstract void RemoveHand(Hand hand);
    public abstract void GetSolvedTransform(ReadonlyList<Hand> hands, out Vector3 position, out Quaternion rotation);
  }
}
