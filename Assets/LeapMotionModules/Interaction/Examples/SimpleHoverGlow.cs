using Leap.Unity;
using Leap.Unity.UI.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(InteractionBehaviour))]
public class SimpleHoverGlow : MonoBehaviour {

  private Material _material;
  private int _emissionPropertyId;

  private InteractionBehaviour _intObj;

  void Start() {
    _intObj = GetComponent<InteractionBehaviour>();

    Renderer renderer = GetComponent<Renderer>();
    if (renderer == null) {
      renderer = GetComponentInChildren<Renderer>();
    }
    if (renderer != null) {
      _material = renderer.material;
      _emissionPropertyId = Shader.PropertyToID("_EmissionColor");
    }
  }

  void Update() {
    if (_material != null) {
      if (_intObj.isSuspended) {
        _material.SetColor(_emissionPropertyId, new Color(0.7F, 0F, 0F, 1F));
      }
      else if (_intObj.isHovered) {
        float glow = Vector3.Distance(_intObj.closestHoveringHand.PalmPosition.ToVector3(), this.transform.position).Map(0F, 0.2F, 1F, 0.4F);
        _material.SetColor(_emissionPropertyId, new Color(glow, glow, glow, 1F));
      }
    }
  }

}
