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
using System.Collections.Generic;

namespace Leap.Unity {
  public class KeyEnableGameObjects : MonoBehaviour {
    public List<GameObject> targets;
    [Header("Controls")]
    public KeyCode unlockHold = KeyCode.RightShift;
    public KeyCode toggle = KeyCode.T;

    // Update is called once per frame
    void Update() {
      if (unlockHold != KeyCode.None &&
          !Input.GetKey(unlockHold)) {
        return;
      }
      if (Input.GetKeyDown(toggle)) {
        for (int i = 0; i < targets.Count; i++) {
          targets[i].SetActive(!targets[i].activeSelf);
        }
      }
    }
  }
}
