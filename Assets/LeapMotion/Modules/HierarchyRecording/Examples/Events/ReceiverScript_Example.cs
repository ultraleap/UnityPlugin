/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

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
