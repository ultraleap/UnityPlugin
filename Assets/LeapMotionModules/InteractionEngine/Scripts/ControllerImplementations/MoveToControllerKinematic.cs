using UnityEngine;
using UnityEngine.Assertions;

namespace Leap.Unity.Interaction {

  /**
  * The MoveToControllerKinematic class moves the held object into position
  * after setting its rigid body to be kinematic.
  * @since 4.1.4
  */
  public class MoveToControllerKinematic : IMoveToController {
    /** Moves the object kinematically. */
    public override void MoveTo(ReadonlyList<Hand> hands, PhysicsMoveInfo info, Vector3 solvedPosition, Quaternion solvedRotation) {
      if (info.shouldTeleport) {
        _obj.warper.Teleport(solvedPosition, solvedRotation);
      } else {
        _obj.rigidbody.MovePosition(solvedPosition);
        _obj.rigidbody.MoveRotation(solvedRotation);
      }
    }

    /** Sets the rigid body's IsKinematic property to true. */
    public override void SetGraspedState() {
      _obj.rigidbody.isKinematic = true;
    }

    /** Does nothing in this implementation. */
    public override void OnGraspBegin() { }

    /** Does nothing in this implementation. */
    public override void OnGraspEnd() { }

    /** Validates that the rigid body IsKinematic flag is still true. */
    public override void Validate() {
      base.Validate();

      if (_obj.IsBeingGrasped) {
        Assert.IsTrue(_obj.rigidbody.isKinematic,
                      "Object must be kinematic when being grasped.");
      }
    }
  }
}
