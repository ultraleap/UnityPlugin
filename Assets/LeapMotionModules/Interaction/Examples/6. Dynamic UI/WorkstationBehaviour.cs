using Leap.Unity.Animation;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Interaction {

  [RequireComponent(typeof(InteractionBehaviour))]
  [RequireComponent(typeof(AnchorableBehaviour))]
  public class WorkstationBehaviour : MonoBehaviour {

    public TransformTweenBehaviour workstationModeTween;

    private InteractionBehaviour _intObj;
    private AnchorableBehaviour _anchObj;

    void OnValidate() {
      refreshRequiredComponents();
    }

    void Start() {
      refreshRequiredComponents();

      if (!_anchObj.tryAnchorNearestOnGraspEnd) {
        Debug.LogWarning("WorkstationBehaviour expects its AnchorableBehaviour's tryAnchorNearestOnGraspEnd property to be enabled.", this.gameObject);
      }
    }

    private void refreshRequiredComponents() {
      _intObj = GetComponent<InteractionBehaviour>();
      _anchObj = GetComponent<AnchorableBehaviour>();

      _intObj.OnObjectGraspBegin += onObjectGraspBegin;

      _anchObj.OnPostTryAnchorOnGraspEnd += onPostObjectGraspEnd;
    }

    void OnDestroy() {
      _intObj.OnObjectGraspBegin -= onObjectGraspBegin;

      _anchObj.OnPostTryAnchorOnGraspEnd -= onPostObjectGraspEnd;
    }

    private void onObjectGraspBegin(List<InteractionHand> hands) {
      // Workstation mode ends if the WorkstationBehaviour itself is grasped.
      // (If we're already not in workstation mode, it's a no-op.)
      if (workstationModeTween != null) workstationModeTween.PlayBackward();
    }

    private void onPostObjectGraspEnd(AnchorableBehaviour anchObj) {
      if (_anchObj.preferredAnchor == null) {
        // The attachment attempt failed, so enter workstation mode!
        if (workstationModeTween != null) workstationModeTween.PlayForward();
      }
    }

  }

}