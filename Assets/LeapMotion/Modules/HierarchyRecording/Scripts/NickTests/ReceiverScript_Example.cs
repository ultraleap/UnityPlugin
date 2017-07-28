using Leap.Unity.Attributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReceiverScript_Example : MonoBehaviour {

  private Material _materialInstance;

  [EditTimeOnly, SerializeField]
  private Color _currentColor;
  public Color currentColor {
    get { return _currentColor; }
    set {
      if (_materialInstance != null) {
        _materialInstance.color = value;
        _currentColor = value;
      }
    }
  }

  private void Start() {
    _materialInstance = GetComponent<Renderer>().material;
    currentColor = Color.white;
  }

  public void SetColorEvent(object colorArg) {
    currentColor = (Color)colorArg;
  }

}
