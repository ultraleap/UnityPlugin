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

public class FlameControl : MonoBehaviour {
  public GameObject CurrentTarget = null;

  public void SetTarget (GameObject target) {
    CurrentTarget = target;
  }

  public void LightFire () {
    if (CurrentTarget != null) {
      for (int c = 0; c < CurrentTarget.transform.childCount; c++) {
        CurrentTarget.transform.GetChild(c).gameObject.SetActive(true);
      }
    }
  }

  public void PutOutFire () {
    if (CurrentTarget != null) {
      for (int c = 0; c < CurrentTarget.transform.childCount; c++) {
        CurrentTarget.transform.GetChild(c).gameObject.SetActive(false);
      }
    }
  }
  
  public void ToggleFire () {
    if (CurrentTarget != null) {
      for (int c = 0; c < CurrentTarget.transform.childCount; c++) {
        CurrentTarget.transform.GetChild(c).gameObject.SetActive(!CurrentTarget.transform.GetChild(c).gameObject.activeInHierarchy);
      }
    }
  }
}
