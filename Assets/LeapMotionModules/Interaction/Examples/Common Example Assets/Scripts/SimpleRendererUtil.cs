/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Examples {

  [RequireComponent(typeof(Renderer))]
  public class SimpleRendererUtil : MonoBehaviour {

    public Color activationColor = Color.yellow;

    private Renderer _renderer;
    private Material _materialInstance;

    private Color _originalColor;

    void Start() {
      _renderer = GetComponent<Renderer>();
      _materialInstance = _renderer.material;
      _originalColor = _materialInstance.color;
    }

    public void SetToActivationColor() {
      _materialInstance.color = activationColor;
    }

    public void SetToOriginalColor() {
      _materialInstance.color = _originalColor;
    }

    public void ShowRenderer() {
      _renderer.enabled = true;
    }

    public void HideRenderer() {
      _renderer.enabled = false;
    }

  }

}
