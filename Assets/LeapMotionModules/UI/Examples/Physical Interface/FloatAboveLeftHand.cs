using Leap;
using Leap.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloatAboveLeftHand : MonoBehaviour {

  public Rigidbody physicalInterfacePanel;

  void FixedUpdate() {
    Hand leftHand = Hands.Left;
    if (leftHand != null) {
      Vector3 verticalOffset = Vector3.up * 0.2F;
      Vector3 newPosition = Vector3.Lerp(physicalInterfacePanel.position, leftHand.PalmPosition.ToVector3() + verticalOffset, 10F * Time.fixedDeltaTime);
      physicalInterfacePanel.transform.position = newPosition;
      Quaternion newRotation = Quaternion.LookRotation((this.transform.position - verticalOffset) - Camera.main.transform.position);
      physicalInterfacePanel.transform.rotation = newRotation;
    }
  }

}
