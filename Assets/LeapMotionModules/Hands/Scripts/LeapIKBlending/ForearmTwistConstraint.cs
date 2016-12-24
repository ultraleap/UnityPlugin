using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ForearmTwistConstraint : MonoBehaviour {
  public Transform Elbow;
  public Transform Shoulder;


  // Use this for initialization
  void Start () {
    
  }
  
  // Update is called once per frame
  void Update () {
    Quaternion zeroX = new Quaternion(-1f * Elbow.localRotation.x, transform.localRotation.y, transform.localRotation.z, transform.localRotation.w);
    transform.localRotation = zeroX;
  }
}
