using UnityEngine;
using Leap.Unity.Interaction;

namespace Leap.Unity.Examples {

  using IntObj = InteractionBehaviour;

  [AddComponentMenu("")]
  public class SwapGraspExample : MonoBehaviour {

    public IntObj objA, objB;

    public InteractionButton swapButton;

    private bool _swapScheduled = false;

    void Start() {
      swapButton.OnUnpress += scheduleSwap;

      // Wait for just after the PhysX update to swap a grasp;
      // this allows the swapped object to inherit the _latest_ rigidbody position and
      // rotation from the original held object (which needs the PhysX update to receive
      // scheduled force / MovePosition / MoveRotation changes from the grasped movement
      // system).
      PhysicsCallbacks.OnPostPhysics += onPostPhysics;
    }

    private void scheduleSwap() {
      _swapScheduled = true;
    }

    private void onPostPhysics() {
      //Swapping when both objects are grasped is unsupported
      if(objA.isGrasped && objB.isGrasped) { return; }
  
      if (_swapScheduled && (objA.isGrasped || objB.isGrasped)) {

        // Swap "a" for "b"; a will be whichever object is the grasped one.
        IntObj a, b;
        if (objA.isGrasped) {
          a = objA;
          b = objB;
        }
        else  {
          a = objB;
          b = objA;
        }

        // (Optional) Remember B's pose and motion to apply to A post-swap.
        var bPose = new Pose(b.rigidbody.position, b.rigidbody.rotation);
        var bVel = b.rigidbody.velocity;
        var bAngVel = b.rigidbody.angularVelocity;

        // Match the rigidbody pose of the originally held object before swapping.
        // If it exists, always use the latestScheduledGraspPose to perform a SwapGrasp!
        // This prevents subtle slippage with non-kinematic objects that may experience
        // gravity forces, drag, or hit other objects, which can leak into the new
        // grasping pose when the SwapGrasp is performed.
        if (a.latestScheduledGraspPose.HasValue) {
          b.rigidbody.position = a.latestScheduledGraspPose.Value.position;
          b.rigidbody.rotation = a.latestScheduledGraspPose.Value.rotation;
        }
        else {
          b.rigidbody.position = a.rigidbody.position;
          b.rigidbody.rotation = a.rigidbody.rotation;
        }

        // Swap!
        a.graspingController.SwapGrasp(b);

        // Move A over to where B was, and for fun, let's give it B's motion as well.
        a.rigidbody.position = bPose.position;
        a.rigidbody.rotation = bPose.rotation;
        a.rigidbody.velocity = bVel;
        a.rigidbody.angularVelocity = bAngVel;
      }

      _swapScheduled = false;
    }
  }

}

