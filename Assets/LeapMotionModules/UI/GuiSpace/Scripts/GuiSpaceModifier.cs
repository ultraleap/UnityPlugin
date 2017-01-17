using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity.Query;

namespace Leap.Unity.Gui.Space {

  [ExecuteInEditMode]
  [RequireComponent(typeof(Renderer))]
  public class GuiSpaceModifier : MonoBehaviour {

    private GuiSpace _parentSpace;
    private Renderer _renderer;

    void OnEnable() {
      UpdateSpace();
    }

    void OnDisable() {
      GuiSpace.ResetRenderer(_renderer);
    }

    private static List<GuiSpace> _spaces = new List<GuiSpace>();
    public void UpdateSpace() {
      GetComponentsInParent<GuiSpace>(true, _spaces);
      _parentSpace = _spaces.Query().FirstOrDefault(s => s.enabled);

      _renderer = GetComponent<Renderer>();

      if (_parentSpace != null && _parentSpace.enabled && enabled) {
        _parentSpace.UpdateRenderer(_renderer);
      } else {
        GuiSpace.ResetRenderer(_renderer);
      }
    }

    public void SetParent(Transform parent) {
      _parentSpace = GetComponentInParent<GuiSpace>();
      _parentSpace.UpdateRenderer(_renderer);
    }
  }
}
