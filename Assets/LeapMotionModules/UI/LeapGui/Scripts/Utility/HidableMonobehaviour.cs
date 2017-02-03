using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HidableMonobehaviour : MonoBehaviour {
  protected virtual void OnValidate() {
    //hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
  }
}
