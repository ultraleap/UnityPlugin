using Leap.Unity.UI.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Paint {

  [RequireComponent(typeof(InteractionBehaviour))]
  public class BuildingPrimitive : MonoBehaviour {

    public float maxHoverReach = 0.1F;
    public AnimationCurve hoverReachFromDistance;

    private InteractionBehaviour _intObj;

    void Start() {
      _intObj = GetComponent<InteractionBehaviour>();
    }

    void Update() {
      float reachTargetAmount = 0F;
      Vector3 towardsHand = Vector3.zero;
      if (_intObj.isHovered) {
        Hand hoveringHand = _intObj.closestHoveringHand;
        reachTargetAmount = hoverReachFromDistance.Evaluate(
                              Vector3.Distance(hoveringHand.PalmPosition.ToVector3(), this.transform.parent.position)
                            );
        towardsHand = hoveringHand.PalmPosition.ToVector3() - this.transform.parent.position;
      }

      Vector3 targetPosition = this.transform.parent.position + towardsHand * maxHoverReach * reachTargetAmount;

      this.transform.position = Vector3.Lerp(this.transform.position, targetPosition, 5 * Time.deltaTime);
    }

  }

}