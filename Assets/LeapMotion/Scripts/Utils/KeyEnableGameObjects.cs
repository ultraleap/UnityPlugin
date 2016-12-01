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