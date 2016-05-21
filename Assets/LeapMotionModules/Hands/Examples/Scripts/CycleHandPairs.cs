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
      Debug.Log(value);
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
    disableAllGroups();
    CurrentGroup = 0;
  }
  
  // Update is called once per frame
  void Update () {

    if (Input.GetKeyUp(KeyCode.RightArrow)) {
      if (CurrentGroup < GroupNames.Length - 1) {
        //CurrentGroup++;
        StartCoroutine(WaitToPlus());
      }
    }
    if (Input.GetKeyUp(KeyCode.LeftArrow)) {
      if (CurrentGroup > 0) {
        //CurrentGroup--;
        StartCoroutine(WaitToMinus());
      }
    }
    for (int i = 0; i < keyCodes.Length; i++) {
      if (Input.GetKeyDown(keyCodes[i])) {
        //handPool.ToggleGroup(GroupNames[i]);
        StartCoroutine(waitToSwitch(i));
      }
      // check for errors. 
    }
    if(Input.GetKeyUp(KeyCode.Alpha0)){
      disableAllGroups();
    }
  }
  private IEnumerator WaitToPlus () {
    yield return new WaitForEndOfFrame();
    CurrentGroup++;
  }
  private IEnumerator WaitToMinus() {
    yield return new WaitForEndOfFrame();
    CurrentGroup--;
  }
  private IEnumerator waitToSwitch(int group) {
    yield return new WaitForEndOfFrame();
    handPool.ToggleGroup(GroupNames[group]);
  }

  private void disableAllGroups() {
    for (int i = 0; i < GroupNames.Length; i++) {
      handPool.DisableGroup(GroupNames[i]);
    }
  }

}
