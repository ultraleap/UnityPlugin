using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Throttle : MonoBehaviour {

  public Spaceship spaceship;

  private float _correctiveStrength = 10F;
  private float _thrusterStrength = 10F;
  private float _throttleAmount = 0F;

  public void SetThrottleAmount(float amount) {
    _throttleAmount = Mathf.Clamp01(amount);
  }

  void FixedUpdate() {
    Vector3 curVelocity = spaceship.ShipAlignedVelocity;
    Vector3 targetVelocity = Vector3.forward * _throttleAmount * _thrusterStrength;
    Vector3 correctiveForce = (targetVelocity - curVelocity) * _correctiveStrength;

    spaceship.AddShipAlignedForce(correctiveForce);
  }

}
