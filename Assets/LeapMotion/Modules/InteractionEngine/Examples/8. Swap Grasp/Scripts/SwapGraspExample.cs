using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity.Interaction;

[AddComponentMenu("")]
public class SwapGraspExample : MonoBehaviour {

  public InteractionBehaviour objA, objB;
  public float startSwapDist = 0.03f;
  public float endSwapDist = 0.1f;

  private IEnumerator Start() {

    while (true) {
      yield return new WaitUntil(() => Vector3.Distance(objA.transform.position, objB.transform.position) < startSwapDist);

      var graspingA = new List<InteractionController>();
      var graspingB = new List<InteractionController>();

      foreach (var controller in objA.graspingControllers) {
        graspingA.Add(controller);
      }
      foreach (var controller in objB.graspingControllers) {
        graspingB.Add(controller);
      }

      foreach (var controller in graspingA) {
        controller.SwapGrasp(objB);
      }
      foreach (var controller in graspingB) {
        controller.SwapGrasp(objA);
      }

      yield return new WaitUntil(() => Vector3.Distance(objA.transform.position, objB.transform.position) > endSwapDist);
    }
  }
}
