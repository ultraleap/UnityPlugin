/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System;
using UnityEngine;

namespace Leap.Unity.Recording {
  using Attributes;

  public class StateMachine : MonoBehaviour {

    public bool enableManualTransition = false;

    [DisableIf("enableManualTransition", isEqualTo: false)]
    public KeyCode manualTransitionKey = KeyCode.F5;

    public GameObject activeState {
      get {
        for (int i = 0; i < transform.childCount; i++) {
          var child = transform.GetChild(i).gameObject;
          if (child.activeInHierarchy) {
            return child;
          }
        }
        return null;
      }
    }

    private void Awake() {
      int enabledCount = 0;
      for (int i = 0; i < transform.childCount; i++) {
        if (transform.GetChild(i).gameObject.activeSelf) {
          enabledCount++;
        }
      }

      //If there is not one state currently enabled, disable all but the first
      if (enabledCount != 1) {
        for (int i = 0; i < transform.childCount; i++) {
          transform.GetChild(i).gameObject.SetActive(i == 0);
        }
      }
    }

    private void Update() {
      if (enableManualTransition && Input.GetKeyDown(manualTransitionKey)) {
        for (int i = 0; i < transform.childCount; i++) {
          var child = transform.GetChild(i).gameObject;
          if (child.activeSelf) {
            var t = child.GetComponent<TransitionBehaviour>();
            if (t != null) {
              t.Transition();
              break;
            }
          }
        }
      }
    }
  }
}
