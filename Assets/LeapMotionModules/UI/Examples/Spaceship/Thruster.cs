using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Thruster : MonoBehaviour {

  public Spaceship spaceship;

  private bool _activated = false;
  private float _thrusterStrength = 5F;

  public void Activate() {
    _activated = true;
  }

  public void Deactivate() {
    _activated = false;
  }

  void FixedUpdate() {
    if (_activated) {
      spaceship.AddForceAtPosition(-this.transform.forward * _thrusterStrength, this.transform.position);
    }
  }

}
