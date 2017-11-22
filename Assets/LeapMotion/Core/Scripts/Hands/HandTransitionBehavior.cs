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

namespace Leap.Unity {
  /**A Component to be attached to a HandModelBase to handle starting and ending of tracking */
  public abstract class HandTransitionBehavior : MonoBehaviour {

    protected abstract void HandReset();
    protected abstract void HandFinish();
    protected HandModelBase handModelBase;
    protected virtual void Awake(){
      handModelBase = GetComponent<HandModelBase>();
      if (handModelBase == null) {
        Debug.LogWarning("HandTransitionBehavior components require a HandModelBase component attached to the same GameObject");
        return;
      }
      handModelBase.OnBegin += HandReset;
      handModelBase.OnFinish += HandFinish;
    }
    protected virtual void OnDestroy() {
      HandModelBase handModelBase = GetComponent<HandModelBase>();
      if (handModelBase == null) {
        Debug.LogWarning("HandTransitionBehavior components require a HandModelBase component attached to the same GameObject");
        return;
      }
      handModelBase.OnBegin -= HandReset;
      handModelBase.OnFinish -= HandFinish;
    }
  } 
}
