using Leap.Unity.Attributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LinearSpring : SpringBase {

  public MinimalBody toPull;
  public float springCoefficient = 1F;
  public float springLength = 0.05F;

  private float friction = 0.1F;

  private Vector3 _lastPosition;

  void Start() {
    _lastPosition = toPull.transform.position;
  }

  void Update() {
    Vector3 displacement = this.transform.position - toPull.transform.position;
    float distanceToSpring = displacement.magnitude;

    Vector3 springForce = displacement * springCoefficient;
    Vector3 newPosition = toPull.transform.position + springForce * Time.deltaTime;

    float impendingBodySpringSpeed = Vector3.Dot((newPosition - _lastPosition) / Time.deltaTime, displacement.normalized);
    //float maxSpeed = (distanceToSpring / springLength) / Time.deltaTime;
    //float dampingSpeedAdjustment = maxSpeed - impendingBodySpringSpeed;
    //Vector3 dampingAdjustment = Vector3.zero;
    //if (dampingSpeedAdjustment < 0) {
    //  // Need to slow down for damping!
    //  dampingAdjustment = -displacement.normalized * Mathf.Abs(dampingSpeedAdjustment);
    //}
    Vector3 frictionForce = (-displacement.normalized * friction * impendingBodySpringSpeed);

    toPull.transform.position += (springForce + frictionForce) * Time.deltaTime;
    _lastPosition = toPull.transform.position;
  }

}
