/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
using System.Collections;
using System;
using Leap;

namespace Leap.Unity{
  public class HandEnableDisable : HandTransitionBehavior {
    protected override void Awake() {
      base.Awake();
      gameObject.SetActive(false);
    }

  	protected override void HandReset() {
      gameObject.SetActive(true);
    }

    protected override void HandFinish() {
      gameObject.SetActive(false);
    }

  }
}
