using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity.UI.Interaction;

[RequireComponent(typeof(InteractionBehaviour))]
public class InteractionSpring : MonoBehaviour {
  InteractionBehaviour beh;
  void Start() {
    beh = GetComponent<InteractionBehaviour>();
  }
	void FixedUpdate () {
    beh.rigidbody.AddForce(Vector3.Scale(Vector3.back, beh.transform.localPosition)/Time.fixedDeltaTime * 0.01f, ForceMode.VelocityChange);
	}
}
