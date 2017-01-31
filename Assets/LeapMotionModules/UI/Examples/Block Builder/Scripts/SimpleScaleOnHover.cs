using Leap;
using Leap.Unity;
using Leap.Unity.UI.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleScaleOnHover : MonoBehaviour {

  private InteractionBehaviour _interaction;
  private Vector3 _baseScale;
  private float _curScale = 1F;
  private float _targetScale = 1F;

  private int _hoverCount = 0;
  private float _closestHoverDistance;
  private int _primaryHoverCount = 0;
  private float _primaryHoverDistance;

  void Start() {
    _interaction = GetComponent<InteractionBehaviour>();
    if (_interaction != null) {
      _interaction.OnPrimaryHoverBeginEvent += OnPrimaryHoverBegin;
      _interaction.OnPrimaryHoverStayEvent  += OnPrimaryHoverStay;
      _interaction.OnPrimaryHoverEndEvent   += OnPrimaryHoverEnd;
      _interaction.OnHoverBeginEvent        += OnHoverBegin;
      _interaction.OnHoverStayEvent         += OnHoverStay;
      _interaction.OnHoverEndEvent          += OnHoverEnd;
    }
    _baseScale = this.transform.localScale;
  }

  private void OnPrimaryHoverBegin(Hand hand) {
    _primaryHoverCount++;
  }

  private void OnPrimaryHoverStay(Hand hand) {
    _primaryHoverDistance = Vector3.Distance(hand.AttentionPosition(), this.transform.position);
  }

  private void OnPrimaryHoverEnd(Hand hand) {
    _primaryHoverCount--;
  }

  private void OnHoverBegin(Hand hand) {
    _hoverCount++;
    if (_hoverCount == 1) {
      _closestHoverDistance = Vector3.Distance(hand.AttentionPosition(), this.transform.position);
    }
  }

  private void OnHoverStay(Hand hand) {
    float distanceTest = Vector3.Distance(hand.AttentionPosition(), this.transform.position);
    if (distanceTest < _closestHoverDistance) {
      _closestHoverDistance = distanceTest;
    }
  }

  private void OnHoverEnd(Hand hand) {
    _hoverCount--;
  }

  void Update() {
    if (_hoverCount > 0) {
      _targetScale = _closestHoverDistance.Map(0.12F, 0.4F, 1.1F, 1F);
    }
    else {
      _targetScale = 1F;
    }

    if (_primaryHoverCount > 0 && _primaryHoverDistance < 0.1F) {
      _targetScale = 1.3F;
    }

    _closestHoverDistance = 10000F;
    _curScale = Mathf.Lerp(_curScale, _targetScale, 10F * Time.deltaTime);
    this.transform.localScale = _baseScale * _curScale;

    if (_hoverCount < 0) {
      Debug.LogError("Something's wrong with the hover logic!!! Hover count < 0");
    }
    if (_primaryHoverCount < 0) {
      Debug.LogError("Something's wrong with the hover logic!!! Primary hover count < 0");
    }
  }

}
