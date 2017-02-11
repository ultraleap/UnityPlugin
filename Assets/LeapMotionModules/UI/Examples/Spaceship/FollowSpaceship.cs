using Leap.Unity.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowSpaceship : MonoBehaviour {

  public Spaceship spaceship;
  
  private Vector3 _positionFromSpaceship;
  private Quaternion _rotationFromSpaceship;

  void Start() {
    _positionFromSpaceship = this.transform.position - spaceship.transform.position;
    _rotationFromSpaceship = this.transform.rotation * Quaternion.Inverse(spaceship.transform.rotation);
  }

  void Update() {
    this.transform.position = spaceship.transform.TransformPoint(_positionFromSpaceship);
    this.transform.rotation = spaceship.transform.rotation * _rotationFromSpaceship;
  }

}
