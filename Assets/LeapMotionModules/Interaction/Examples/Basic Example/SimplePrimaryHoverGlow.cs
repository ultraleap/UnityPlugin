using Leap.Unity.UI.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(InteractionBehaviour))]
public class SimplePrimaryHoverGlow : MonoBehaviour {

  private InteractionBehaviour _intObj;

  void Start() {
    _intObj = GetComponent<InteractionBehaviour>();
  }

  void Update() {
    if (_intObj.isGrasped) {
      Turn(Color.red);
    }
    else {
      if (_intObj.isPrimaryHovered) {
        // var hand = _intObj.primaryHoveringHand;
        TurnWhite();
      }
      else {
        TurnBlack();
      }
    }
  }

  private Material _matInstance;

  private void TurnWhite() {
    var renderer = GetComponent<Renderer>();
    if (_matInstance == null) _matInstance = renderer.material;
    else {
      _matInstance.color = Color.white;
    }
  }

  private void TurnBlack() {
    var renderer = GetComponent<Renderer>();
    if (_matInstance == null) _matInstance = renderer.material;
    else {
      _matInstance.color = Color.black;
    }
  }

  private void Turn(Color c) {
    var renderer = GetComponent<Renderer>();
    if (_matInstance == null) _matInstance = renderer.material;
    else {
      _matInstance.color = c;
    }
  }

}
