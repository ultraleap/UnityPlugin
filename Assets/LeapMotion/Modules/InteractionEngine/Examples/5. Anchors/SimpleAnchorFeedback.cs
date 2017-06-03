/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using Leap.Unity.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Examples {

  [RequireComponent(typeof(Anchor))]
  [AddComponentMenu("")]
  public class SimpleAnchorFeedback : MonoBehaviour {

    public Transform scaleTarget;

    private Anchor _anchor;

    private Vector3 _initScaleVector;
    private float _curScale = 1F;

    void Start() {
      _anchor = GetComponent<Anchor>();

      _initScaleVector = scaleTarget.transform.localScale;
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

      scaleTarget.transform.localScale = _curScale * _initScaleVector;
    }

  }

}
