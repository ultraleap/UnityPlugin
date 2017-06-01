using Leap.Unity;
using Leap.Unity.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowSpaceship : MonoBehaviour {

  public Spaceship spaceship;

  public InteractionManager manager;
  
  private Vector3    _positionFromSpaceship;
  //private Quaternion _rotationFromSpaceship;

  void Start() {
    _positionFromSpaceship = this.transform.position - spaceship.transform.position;
    //_rotationFromSpaceship = this.transform.rotation * Quaternion.Inverse(spaceship.rigidbody.rotation);

    spaceship.OnMovement += onSpaceshipMovement;
  }

  private void onSpaceshipMovement() {
    this.transform.position = spaceship.transform.position + _positionFromSpaceship;

    Hands.Provider.ReTransformFrames();
  }

}
