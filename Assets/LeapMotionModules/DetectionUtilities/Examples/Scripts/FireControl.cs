using UnityEngine;
using System.Collections;

public class FireControl : MonoBehaviour {

  public void ToggleChildren(GameObject parent){
    for(int c = 0; c < parent.transform.childCount; c++){
      parent.transform.GetChild(c).gameObject.SetActive(!parent.transform.GetChild(c).gameObject.activeInHierarchy);
    }
  }
}
