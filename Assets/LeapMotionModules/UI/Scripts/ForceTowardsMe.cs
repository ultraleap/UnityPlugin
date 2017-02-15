using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ForceTowardsMe : MonoBehaviour {

  public Rigidbody toBeForced;

  private float _force = 1600F;
  private float _friction = 100F;
  private float _drag = 500F;

  void Update() {
    Vector3 displacement = (this.transform.position - toBeForced.position);
    toBeForced.AddForce(displacement * _force * Time.deltaTime);
    toBeForced.AddForce(toBeForced.velocity.magnitude    * -toBeForced.velocity.normalized * _friction * Time.deltaTime);
    toBeForced.AddForce(toBeForced.velocity.sqrMagnitude * -toBeForced.velocity.normalized * _drag * Time.deltaTime);
  }

}
