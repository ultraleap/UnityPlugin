using UnityEngine;
using System.Collections;
using Leap.Unity;

public class CycleHandPairs : MonoBehaviour {
  public HandPool HandPool;
  public string[] GroupNames;
  private int currentGroup;
  public int CurrentGroup {
    get { return currentGroup; }
    set {
      disableAllGroups();
      currentGroup = value;
      HandPool.EnableGroup(GroupNames[value]);
    }
  }
  private KeyCode[] keyCodes = {
         KeyCode.Alpha1,
         KeyCode.Alpha2,
         KeyCode.Alpha3,
         KeyCode.Alpha4,
         KeyCode.Alpha5,
         KeyCode.Alpha6,
         KeyCode.Alpha7,
         KeyCode.Alpha8,
         KeyCode.Alpha9,
     };

  // Use this for initialization
  void Start () {
    HandPool = GetComponent<HandPool>();
    disableAllGroups();
    CurrentGroup = 0;
  }
  
  // Update is called once per frame
  void Update () {
    if (Input.GetKeyUp(KeyCode.RightArrow)) {
      if (CurrentGroup < GroupNames.Length - 1) {
        CurrentGroup++;
      }
    }
    if (Input.GetKeyUp(KeyCode.LeftArrow)) {
      if (CurrentGroup > 0) {
        CurrentGroup--;
      }
    }
    for (int i = 0; i < keyCodes.Length; i++) {
      if (Input.GetKeyDown(keyCodes[i])) {
        HandPool.ToggleGroup(GroupNames[i]);
      }
    }
    if(Input.GetKeyUp(KeyCode.Alpha0)){
      disableAllGroups();
    }
  }

  private void disableAllGroups() {
    for (int i = 0; i < GroupNames.Length; i++) {
      HandPool.DisableGroup(GroupNames[i]);
    }
  }

}
