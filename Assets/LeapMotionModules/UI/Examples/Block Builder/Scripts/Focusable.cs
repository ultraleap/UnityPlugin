using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Focusable : MonoBehaviour {

  private bool _isFocused = false;
  public bool IsFocused {
    get {
      return _isFocused;
    }
  }

  void Start() {
    FocusManager.Add(this);
  }

}
