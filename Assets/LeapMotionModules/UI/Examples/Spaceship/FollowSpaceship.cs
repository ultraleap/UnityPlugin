using Leap.Unity.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowSpaceship : MonoBehaviour {

  public Rigidbody spaceshipBody;

  void Start() {
    PhysicsCallbacks.OnPrePhysics += OnPrePhysics;
  }

  private void OnPrePhysics() {
    this.transform.position = spaceshipBody.position;
    this.transform.rotation = spaceshipBody.rotation;
  }

  void Update() {
    this.transform.position = spaceshipBody.position;
    this.transform.rotation = spaceshipBody.rotation;
  }

}
