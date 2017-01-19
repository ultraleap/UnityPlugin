using Leap.Unity.Attributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LinearSpring : SpringBase {

  public MinimalBody toPull;
  public float springCoefficient = 1F;

  void Update() {
    Vector3 springForce = (this.transform.position - toPull.transform.position) * springCoefficient;
    toPull.transform.position += springForce * Time.deltaTime;
  }

}
