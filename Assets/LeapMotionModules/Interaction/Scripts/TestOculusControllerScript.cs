using Leap.Unity.RuntimeGizmos;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR;

public class TestOculusControllerScript : MonoBehaviour, IRuntimeGizmoComponent {

  private Vector3 _leftPosition;
  private Quaternion _leftRotation;

  private Vector3 _rightPosition;
  private Quaternion _rightRotation;

  void Update() {
    Vector3 localLeftHandPosition = InputTracking.GetLocalPosition(VRNode.LeftHand);
    Vector3 localRightHandPosition = InputTracking.GetLocalPosition(VRNode.RightHand);

    _leftPosition = localLeftHandPosition;
    _rightPosition = localRightHandPosition;

    //foreach (var str in Input.GetJoystickNames()) {
    //  Debug.Log(str);
    //}
  }

  public void OnDrawRuntimeGizmos(RuntimeGizmoDrawer drawer) {
    drawer.color = Color.blue;
    drawer.DrawWireSphere(_leftPosition, 0.05F);

    drawer.color = Color.red;
    drawer.DrawWireSphere(_rightPosition, 0.05F);
  }
}
