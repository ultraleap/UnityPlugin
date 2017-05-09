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
