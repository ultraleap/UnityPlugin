using Leap.Unity.UI.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Examples.TransformHandles {

  [RequireComponent(typeof(InteractionBehaviour))]
  public class ColorOnGrab : MonoBehaviour {

    public string materialColorProperty = "_Color";
    public Color grabbedColor = Color.white;
    public string materialEmissionProperty = "_EmissionColor";
    public Color grabbedEmission = Color.white;

    private InteractionBehaviour _interaction;

    private Material _materialInstance;
    private Color _defaultColor;
    private Color _defaultEmission;
    private int _materialColorPropertyId;
    private int _materialEmissionPropertyId;

    void OnValidate() {
      _materialColorPropertyId = Shader.PropertyToID(materialColorProperty);
      _materialEmissionPropertyId = Shader.PropertyToID(materialEmissionProperty);
    }

    void Start() {
      _interaction = GetComponent<InteractionBehaviour>();
      _interaction.OnGraspBegin += onGraspBegin;
      _interaction.OnGraspEnd += onGraspEnd;

      MeshRenderer renderer = GetComponentInChildren<MeshRenderer>();
      if (renderer != null) {
        _materialInstance = renderer.material;
        _materialColorPropertyId = Shader.PropertyToID(materialColorProperty);
        _materialEmissionPropertyId = Shader.PropertyToID(materialEmissionProperty);
        _defaultColor = _materialInstance.GetColor(_materialColorPropertyId);
        _defaultEmission = _materialInstance.GetColor(_materialEmissionPropertyId);
      }
    }

    private void onGraspBegin(List<InteractionHand> hands) {
      _materialInstance.SetColor(_materialColorPropertyId, grabbedColor);
      _materialInstance.SetColor(_materialEmissionPropertyId, grabbedEmission);
    }

    private void onGraspEnd(List<InteractionHand> hands) {
      _materialInstance.SetColor(_materialColorPropertyId, _defaultColor);
      _materialInstance.SetColor(_materialEmissionPropertyId, _defaultEmission);
    }

  }

}