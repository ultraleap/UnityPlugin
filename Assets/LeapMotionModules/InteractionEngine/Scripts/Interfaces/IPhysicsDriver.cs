using UnityEngine;

namespace Leap.Unity.Interaction {

  public abstract class IPhysicsDriver : IControllerBase {
    public abstract void DrivePhysics(ReadonlyList<Hand> hands, PhysicsMoveInfo info, Vector3 solvedPosition, Quaternion solvedRotation);
    public abstract void SetGraspedState();
    public abstract void OnGrasp();
    public abstract void OnUngrasp();
  }

  public struct PhysicsMoveInfo {
    public bool shouldTeleport;
    public float remainingDistanceLastFrame;
  }
}
