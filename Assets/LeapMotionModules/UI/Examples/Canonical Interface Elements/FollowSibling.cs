using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowSibling : MonoBehaviour {

  public Transform sibling;
  public float localPositionFollowScale = 1F;

  // TODO: Make layers that are actually good

  void Update() {
    this.transform.localPosition = sibling.transform.localPosition * localPositionFollowScale;
  }

}
