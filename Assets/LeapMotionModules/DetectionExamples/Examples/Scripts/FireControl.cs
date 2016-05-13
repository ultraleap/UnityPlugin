using UnityEngine;
using System.Collections;

public class FireControl : MonoBehaviour {
  public GameObject CurrentTarget = null;

  public void SetTarget (GameObject target) {
    CurrentTarget = target;
  }

  public void ActivateChildren () {
    if (CurrentTarget != null) {
      for (int c = 0; c < CurrentTarget.transform.childCount; c++) {
        CurrentTarget.transform.GetChild(c).gameObject.SetActive(true);
      }
    }
  }

  public void DeactivateChildren () {
    if (CurrentTarget != null) {
      for (int c = 0; c < CurrentTarget.transform.childCount; c++) {
        CurrentTarget.transform.GetChild(c).gameObject.SetActive(false);
      }
    }
  }
  
  public void ToggleChildren () {
    if (CurrentTarget != null) {
      for (int c = 0; c < CurrentTarget.transform.childCount; c++) {
        CurrentTarget.transform.GetChild(c).gameObject.SetActive(!CurrentTarget.transform.GetChild(c).gameObject.activeInHierarchy);
      }
    }
  }
}
