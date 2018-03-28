/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Recording {

  public class TransitionAfterDelay : TransitionBehaviour {

    public float delay = 1;

    private float _enterTime;

    private void OnEnable() {
      _enterTime = Time.time;
    }

    private void Update() {
      if ((Time.time - _enterTime) > delay) {
        Transition();
      }
    }
  }
}
