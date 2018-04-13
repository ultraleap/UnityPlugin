/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
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

  [AddComponentMenu("")]
  public class SimpleMatchAnchorScaleAndState : MonoBehaviour {

    public AnchorableBehaviour anchObj;

    void Update() {
      if (anchObj != null && anchObj.anchor != null && anchObj.isAttached) {
        anchObj.transform.localScale = anchObj.anchor.transform.localScale;

        anchObj.gameObject.SetActive(anchObj.anchor.gameObject.activeInHierarchy);

        if (!anchObj.gameObject.activeInHierarchy) {
          anchObj.transform.position = anchObj.anchor.transform.position;
          if (anchObj.anchorRotation) anchObj.transform.rotation = anchObj.anchor.transform.rotation;
        }
      }
    }

  }

}
