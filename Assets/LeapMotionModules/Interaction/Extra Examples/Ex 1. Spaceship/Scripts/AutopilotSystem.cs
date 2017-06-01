using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutopilotSystem : MonoBehaviour {

  public Spaceship spaceship;

  private float _targetSpeed = 0F;

  private float _currentSpeed = 0F;
  public float currentSpeed { get { return _currentSpeed; } }

  public void IncreaseSpeed() {
    _targetSpeed += 5F;

    Debug.Log("OK! Target speed is currently " + _targetSpeed);
  }

  private float _speedErrorSum = 0F;
  private float _lastSpeedError = 0F;

  private float kP = -1.0F;
  private float kI = -0.5F;
  private float kD = -3.0F;

  void Update() {
    // Simple PID on throttle output to get to target speed
    //_currentSpeed = spaceship.ShipAlignedVelocity.z;

    //float speedError = _currentSpeed - _targetSpeed;
    //_speedErrorSum += speedError * Time.deltaTime;
    //float deltaError = (speedError - _lastSpeedError) / Time.deltaTime;

    //float throttleOut = speedError      * kP
    //                  + _speedErrorSum  * kI
    //                  + deltaError      * kD;

    //_lastSpeedError = speedError;

    //// Apply output throttle
    //spaceship.AddForce(throttleOut * Vector3.forward);

    spaceship.velocity = _targetSpeed * Vector3.forward;
  }

}
