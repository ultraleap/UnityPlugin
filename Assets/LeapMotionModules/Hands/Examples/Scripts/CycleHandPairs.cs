using UnityEngine;
using System.Collections;
using Leap.Unity;

[RequireComponent(typeof(HandPool))]
public class CycleHandPairs : MonoBehaviour {
  private HandPool handPool;
  public string[] GroupNames;
  private int currentGroup;
  public int CurrentGroup {
    get { return currentGroup; }
    set {
      disableAllGroups();
      //handPool.DisableGroup(GroupNames[CurrentGroup]);
      currentGroup = value;
      handPool.EnableGroup(GroupNames[value]);
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
    handPool = GetComponent<HandPool>();
  }
  
  // Update is called once per frame
  void LateUpdate () {
    if (Input.GetKeyUp(KeyCode.RightArrow)) {
      if (CurrentGroup < GroupNames.Length -1) {
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
        //handPool.ToggleGroup(GroupNames[i]);
      }

      // check for errors. 
    }
  }

  private void disableAllGroups() {
    for (int i = 0; i < GroupNames.Length; i++) {
      handPool.DisableGroup(GroupNames[i]);
    }
  }
  //show group name on change
  //Disable all
  //arrows disable all
  //0 disables all
  //number toggles group
}
