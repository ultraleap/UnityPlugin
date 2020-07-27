/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
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
