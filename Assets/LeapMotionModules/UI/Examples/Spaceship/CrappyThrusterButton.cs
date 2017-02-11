using Leap;
using Leap.Unity.UI.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrappyThrusterButton : MonoBehaviour {

  public Thruster thruster;

  private InteractionBehaviour _interactionObj;

  private Material _material;
  private Color _normalColor;
  private int _standardShaderEmissionColorId;

  void Start() {
    _interactionObj = GetComponent<InteractionBehaviour>();
    _interactionObj.OnContactBegin += OnContactBegin;
    _interactionObj.OnContactEnd   += OnContactEnd;

    _material = GetComponent<Renderer>().material;
    _standardShaderEmissionColorId = Shader.PropertyToID("_EmissionColor");
    _normalColor = _material.GetColor(_standardShaderEmissionColorId);
  }

  private void OnContactBegin(Hand hand) {
    _material.SetColor(_standardShaderEmissionColorId, Color.white * 0.3F);
    thruster.Activate();
  }

  private void OnContactEnd(Hand hand) {
    _material.SetColor(_standardShaderEmissionColorId, _normalColor);
    thruster.Deactivate();
  }

}
