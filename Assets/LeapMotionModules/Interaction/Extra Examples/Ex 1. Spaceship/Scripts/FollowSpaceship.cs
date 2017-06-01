using Leap.Unity;
using Leap.Unity.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowSpaceship : MonoBehaviour {

  public Spaceship spaceship;

  public InteractionManager manager;
  
  private Vector3    _positionFromSpaceship;
  private Quaternion _rotationFromSpaceship;

  void Start() {
    _positionFromSpaceship = this.transform.position - spaceship.rigidbody.position;
    //_rotationFromSpaceship = this.transform.rotation * Quaternion.Inverse(spaceship.rigidbody.rotation);

    //manager.OnPrePhysicalUpdate += onPrePhysicalUpdate;
    
    //manager.OnPostPhysicalUpdate += onPostPhysicalUpdate;
  }

  private void Update() {
    this.transform.position = spaceship.transform.position + _positionFromSpaceship;

    //this.transform.position = spaceship.transform.TransformPoint(_positionFromSpaceship);
    //this.transform.rotation = spaceship.transform.rotation * _rotationFromSpaceship;

    //Hands.Provider.ReTransformFrames();
  }

  private void onPostPhysicalUpdate() {
    //this.transform.position = (spaceship.rigidbody.position + (spaceship.velocity * Time.fixedDeltaTime)) + _positionFromSpaceship;

    //Hands.Provider.ReTransformFrames();
  }

}
