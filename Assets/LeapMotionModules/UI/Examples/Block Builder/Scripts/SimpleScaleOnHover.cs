using Leap.Unity.UI.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleScaleOnHover : Hoverable {

  private Vector3 _baseLocalScale;
  private float _curScale = 1F;
  private float _targetScale = 1F;

  protected override void Start() {
    base.Start();

    _baseLocalScale = this.transform.localScale;
  }

  void Update() {
    if (primaryHoveringHand != null) {
      _targetScale = 1.4F;
    }
    else if (hoveringHandCount > 0) {
      _targetScale = 1.2F;
    }
    else {
      _targetScale = 1F;
    }

    _curScale = Mathf.Lerp(_curScale, _targetScale, 30F * Time.deltaTime);
    this.transform.localScale = _baseLocalScale * _curScale;
  }

}
