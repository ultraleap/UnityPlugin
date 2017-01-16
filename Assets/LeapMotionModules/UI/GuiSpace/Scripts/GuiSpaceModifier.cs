using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Gui.Space {

  [RequireComponent(typeof(Renderer))]
  public class GuiSpaceModifier : MonoBehaviour {

    private GuiSpace _parentSpace;
    private Renderer _renderer;

    void OnEnable() {
      if (_parentSpace == null) {
        _parentSpace = GetComponentInParent<GuiSpace>();
      }

      _renderer = GetComponent<Renderer>();

      _parentSpace.UpdateRenderer(_renderer);
    }

    void OnDisable() {
      _parentSpace.ResetRenderer(_renderer);
    }

    public void SetParent(Transform parent) {
      _parentSpace = GetComponentInParent<GuiSpace>();
      _parentSpace.UpdateRenderer(_renderer);
    }
  }
}
