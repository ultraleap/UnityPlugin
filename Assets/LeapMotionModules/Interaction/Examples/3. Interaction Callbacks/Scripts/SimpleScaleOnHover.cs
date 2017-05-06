/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using Leap;
using Leap.Unity;
using Leap.Unity.UI.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Examples.TransformHandles {

  [AddComponentMenu("")]
  public class SimpleScaleOnHover : MonoBehaviour {

    private InteractionBehaviour _intObj;
    private Vector3 _baseScale;
    private float _curScale = 1F;
    private float _targetScale = 1F;

    void Start() {
      _intObj = GetComponent<InteractionBehaviour>();

      _baseScale = this.transform.localScale;
    }

    void Update() {
      if (_intObj.isHovered) {
        _targetScale = Vector3.Distance(_intObj.closestHoveringHand.PalmPosition.ToVector3(), this.transform.position)
                              .Map(0.12F, 0.4F, 1.1F, 1F);
      }

      if (_intObj.isPrimaryHovered) {
        _targetScale = 1.3F;
      }

      _curScale = Mathf.Lerp(_curScale, _targetScale, 10F * Time.deltaTime);
      this.transform.localScale = _baseScale * _curScale;
    }

  }

}
