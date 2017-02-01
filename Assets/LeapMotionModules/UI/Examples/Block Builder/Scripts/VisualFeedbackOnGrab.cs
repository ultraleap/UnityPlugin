using Leap;
using Leap.Unity;
using Leap.Unity.UI.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(InteractionBehaviour))]
public class VisualFeedbackOnGrab : MonoBehaviour {

  public string materialColorProperty = "_Color";
  public Color selectedColor = Color.white;
  public string materialEmissionProperty = "_EmissionColor";
  public Color selectedEmission = Color.white;

  private InteractionBehaviour _interaction;

  private Material _materialInstance;
  private Color _defaultColor;
  private Color _defaultEmission;
  private int _materialColorPropertyInt;
  private int _materialEmissionPropertyInt;

  void Start() {
    _interaction = GetComponent<InteractionBehaviour>();
    _interaction.OnGraspBegin += OnGraspBegin;
    _interaction.OnGraspEnd   += OnGraspEnd;

    MeshRenderer renderer = GetComponentInChildren<MeshRenderer>();
    if (renderer != null) {
      _materialInstance = renderer.material;
      _materialColorPropertyInt = Shader.PropertyToID(materialColorProperty);
      _materialEmissionPropertyInt = Shader.PropertyToID(materialEmissionProperty);
      _defaultColor = _materialInstance.GetColor(_materialColorPropertyInt);
      _defaultEmission = _materialInstance.GetColor(_materialEmissionPropertyInt);
    }
  }

  void OnValidate() {
    _materialColorPropertyInt = Shader.PropertyToID(materialColorProperty);
    _materialEmissionPropertyInt = Shader.PropertyToID(materialEmissionProperty);
  }

  private void OnGraspBegin(Hand hand) {
    _materialInstance.SetColor(_materialColorPropertyInt, selectedColor);
    _materialInstance.SetColor(_materialEmissionPropertyInt, selectedEmission);
  }

  private void OnGraspEnd(Hand hand) {
    _materialInstance.SetColor(_materialColorPropertyInt, _defaultColor);
    _materialInstance.SetColor(_materialEmissionPropertyInt, _defaultEmission);
  }

}
