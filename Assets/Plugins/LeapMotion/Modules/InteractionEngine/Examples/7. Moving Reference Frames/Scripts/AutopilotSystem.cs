/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Examples {

  [AddComponentMenu("")]
  public class AutopilotSystem : MonoBehaviour {

    public Spaceship spaceship;

    private float _targetSpeed = 0F;
    private Vector3 _targetTorque = Vector3.zero;

    private float _currentSpeed = 0F;
    public float currentSpeed { get { return _currentSpeed; } }

    public void IncreaseSpeed() {
      _targetSpeed += 5F;
    }

    public void IncreaseTorque() {
      _targetTorque += 36f * Vector3.one;
    }

    public void Stop() {
      _targetSpeed = 0F;
      _targetTorque = Vector3.zero;
    }

    void Update() {
      spaceship.velocity = _targetSpeed * Vector3.forward;
      spaceship.angularVelocity = _targetTorque;
    }

  }

}
