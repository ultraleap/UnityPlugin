using Leap;
using Leap.Unity;
using Leap.Unity.RuntimeGizmos;
using Leap.Unity.UI.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttentionPositionGizmo : MonoBehaviour, IRuntimeGizmoComponent {

  private Vector3 _attentionPosition;

  void Update() {
    Hand hand = Hands.Right;
    if (hand != null) {
      _attentionPosition = hand.AttentionPosition();
    }
  }

  public void OnDrawRuntimeGizmos(RuntimeGizmoDrawer drawer) {
    drawer.color = Color.blue;
    drawer.DrawSphere(_attentionPosition, 0.02F);
  }

}
