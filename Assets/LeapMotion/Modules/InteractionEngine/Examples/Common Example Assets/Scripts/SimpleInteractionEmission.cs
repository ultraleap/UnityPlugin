/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using Leap.Unity.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Examples {

  [AddComponentMenu("")]
  [RequireComponent(typeof(InteractionBehaviour))]
  public class SimpleInteractionEmission : MonoBehaviour {

    public Color nonPrimaryHoverEmission = Color.black;
    public Color primaryHoverEmission = Color.white;

    private Material _material;
    private int _emissionColorId;

    private InteractionBehaviour _intObj;

    private Color _targetColor;

    void Start() {
      var renderer = GetComponentInChildren<Renderer>();
      if (renderer != null) {
        _material = renderer.material;
        _emissionColorId = Shader.PropertyToID("_EmissionColor");
      }

      _intObj = GetComponent<InteractionBehaviour>();
    }

    void Update() {
      _targetColor = nonPrimaryHoverEmission;

      if (_intObj.isPrimaryHovered) {
        _targetColor = primaryHoverEmission;
      }

      _material.SetColor(_emissionColorId, Color.Lerp(_material.GetColor(_emissionColorId), _targetColor, 20F * Time.deltaTime));
    }

  }

}
