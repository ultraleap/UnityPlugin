using Leap.Unity.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.UI.Interaction.Examples {

  public class ColorChooserButton : MonoBehaviour {

    public Renderer colorSource;
    public Renderer panelRenderer;

    private InteractionBehaviour _interactionObj;
    private float _startingLocalZ;
    private float _triggerDepth = 0.009F;
    private bool _isReady = true;
    private Material _panelMaterialInstance;
    private Color _colorOnActivation;

    void Start() {
      _interactionObj = GetComponent<InteractionBehaviour>();
      _startingLocalZ = this.transform.localPosition.z;
      _panelMaterialInstance = panelRenderer.material;
      _colorOnActivation = colorSource.material.color;
    }

    private bool IsPressed { get { return this.transform.localPosition.z - _startingLocalZ > _triggerDepth; } }
    private bool IsReady { get { return _isReady; } }

    void Update() {
      if (!_isReady && !IsPressed) {
        _isReady = true;
      }

      if (_interactionObj.IsPrimaryHovered && IsPressed && IsReady) {
        ActivateButton();
        _isReady = false;
      }
    }

    private void ActivateButton() {
      _panelMaterialInstance.color = new Color(_colorOnActivation.r, _colorOnActivation.g, _colorOnActivation.b, 0.5F);
    }

  }

}