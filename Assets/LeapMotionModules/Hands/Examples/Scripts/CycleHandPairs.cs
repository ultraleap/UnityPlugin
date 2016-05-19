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
      handPool.DisableGroup(GroupNames[CurrentGroup]);
      currentGroup = value;
      handPool.EnableGroup(GroupNames[value]);
    }
  }

  // Use this for initialization
  void Start () {
    handPool = GetComponent<HandPool>();
  }
  
  // Update is called once per frame
  void Update () {
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
  }
  //show group name on change
}
