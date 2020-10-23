/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;

namespace Leap.Unity.Interaction.Tests {

  [AddComponentMenu("")]
  public class TestInteractionStateColor : MonoBehaviour {

    public InteractionBehaviour intObj;

    private Material _mat;

    void Start() {
      if (intObj == null) {
        intObj = GetComponent<InteractionBehaviour>();
      }

      _mat = intObj.GetComponentInChildren<Renderer>().material;
    }

    void Update() {
      if (_mat != null && intObj != null) {
        Color color = Color.white;

        if (intObj.isGrasped) {
          color = Color.green;
        }
        else if (intObj.isPrimaryHovered) {
          color = Color.blue;
        }
        else if (intObj.isHovered) {
          color = Color.cyan;
        }
        else if (intObj.isSuspended) {
          color = Color.red;
        }

        var intButton = intObj as InteractionButton;
        if (intButton != null) {
          if (intButton.isPressed) {
            color = Color.yellow;
          }
        }

        _mat.color = color;
      }
    }

  }

}
