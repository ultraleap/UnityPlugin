using Leap;
using Leap.Unity.UI.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorOnPrimaryHover : MonoBehaviour {

  public Color primaryHoverColor = Color.yellow;

  private Material _materialInstance;
  private Color _initColor;

  private InteractionBehaviour _interactionObj;

  void Start() {
    _materialInstance = GetComponentInChildren<Renderer>().material;
    _initColor = _materialInstance.color;

    _interactionObj = GetComponent<InteractionBehaviour>();
    _interactionObj.OnObjectPrimaryHoverBegin += OnObjectPrimaryHoverBegin;
    _interactionObj.OnObjectPrimaryHoverEnd   += OnObjectPrimaryHoverEnd;
  }

  private void OnObjectPrimaryHoverBegin(Hand hand) {
    _materialInstance.color = primaryHoverColor;
  }

  private void OnObjectPrimaryHoverEnd(Hand hand) {
    _materialInstance.color = _initColor;
  }

}
