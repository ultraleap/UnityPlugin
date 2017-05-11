using Leap.Unity.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Examples {

  [RequireComponent(typeof(Anchor))]
  [AddComponentMenu("")]
  public class SimpleAnchorFeedback : MonoBehaviour {

    //public float preferenceScale = 1.3F;
    //public float anchoredScale = 1.5F;

    private Anchor _anchor;

    private Vector3 _initScaleVector;
    private float _curScale = 1F;

    void Start() {
      _anchor = GetComponent<Anchor>();

      _initScaleVector = this.transform.localScale;
    }

    void Update() {
      float _targetScale = 1F;

      if (_anchor.isPreferred) {
        _targetScale = 1.3F;
      }

      if (_anchor.hasAnchoredObjects) {
        _targetScale = 2.4F;
      }

      _curScale = Mathf.Lerp(_curScale, _targetScale, 20F * Time.deltaTime);

      this.transform.localScale = _curScale * _initScaleVector;
    }

  }

}
