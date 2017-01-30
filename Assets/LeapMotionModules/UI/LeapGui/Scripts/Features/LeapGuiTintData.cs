using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LeapGuiTintData : LeapGuiElementData {

  [SerializeField]
  private Color _tint = Color.white;

  public Color tint {
    get {
      return _tint;
    }
    set {
      _tint = value;
      feature.isDirty = true;
    }
  }
}
