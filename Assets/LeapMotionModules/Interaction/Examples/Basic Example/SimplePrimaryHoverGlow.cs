using Leap.Unity;
using Leap.Unity.RuntimeGizmos;
using Leap.Unity.UI.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(InteractionBehaviour))]
public class SimplePrimaryHoverGlow : MonoBehaviour, IRuntimeGizmoComponent {

  private InteractionBehaviour _intObj;

  private BoxCollider _collider;

  void Start() {
    _intObj = GetComponent<InteractionBehaviour>();

    _collider = GetComponent<BoxCollider>();
  }

  void Update() {
    if (_intObj.isGrasped) {
      Turn(Color.blue);
    }
    else {
      if (_intObj.isPrimaryHovered) {
        // var hand = _intObj.primaryHoveringHand;
        Turn(Color.white);
      }
      else {
        Turn(Color.black);
      }
    }
  }

  private Material _matInstance;

  private void Turn(Color c) {
    var renderer = GetComponent<Renderer>();
    if (_matInstance == null) _matInstance = renderer.material;
    else {
      _matInstance.color = c;
    }
  }

  public void OnDrawRuntimeGizmos(RuntimeGizmoDrawer drawer) {
    drawer.color = Color.blue;
    if (_intObj != null && _collider != null && _intObj.isPrimaryHovered) {
      Vector3 primaryTipPos = _intObj.primaryHoveringFinger.TipPosition.ToVector3();
      drawer.DrawLine(primaryTipPos, Physics.ClosestPoint(primaryTipPos, _collider, this.transform.position, this.transform.rotation));
    }
  }

}
