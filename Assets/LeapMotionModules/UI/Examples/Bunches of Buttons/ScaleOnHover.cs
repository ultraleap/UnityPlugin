using Leap.Unity.UI.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.UI.Examples {

  public class ScaleOnHover : MonoBehaviour {

    private InteractionBehaviour _interactionObj;
    private Vector3 _initScale;
    private float _hoverDistance;
    private float _scaleAmount = 1F;
    private bool _primaryHover;

    void Start() {
      _interactionObj = GetComponent<InteractionBehaviour>();
      _interactionObj.OnObjectHoverStay += OnObjectHoverStay;
      _interactionObj.OnObjectHoverEnd  += OnObjectHoverEnd;
      _interactionObj.OnObjectPrimaryHoverBegin += OnObjectPrimaryHoverBegin;
      _interactionObj.OnObjectPrimaryHoverEnd   += OnObjectPrimaryHoverEnd;

      _initScale = this.transform.localScale;
    }

    private void OnObjectHoverStay(Hand hand) {
      _hoverDistance = Vector3.Distance(hand.PalmPosition.ToVector3(), this.transform.position);
    }

    private void OnObjectHoverEnd(Hand hand) {
      _hoverDistance = 1000F;
    }

    private void OnObjectPrimaryHoverBegin(Hand hand) {
      _primaryHover = true;
    }

    private void OnObjectPrimaryHoverEnd(Hand hand) {
      _primaryHover = false;
    }

    void Update() {
      float scaleTarget;
      if (_primaryHover) {
        scaleTarget = 1.3F;
      }
      else {
        scaleTarget = _hoverDistance.Map(0.1F, 0.3F, 1.1F, 1F);
      }

      _scaleAmount = Mathf.Lerp(_scaleAmount, scaleTarget, 20F * Time.deltaTime);
      this.transform.localScale = _initScale * _scaleAmount;
    }

  }

}
