/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;

namespace Leap.Unity.Examples {

  [AddComponentMenu("")]
  public class LinearSpeedTextBehaviour : MonoBehaviour {

    public TextMesh textMesh;

    public Spaceship ship;

    public string linearSpeedPrefixText;

    public string linearSpeedPostfixText;

    void Update() {
      textMesh.text = linearSpeedPrefixText + ship.shipAlignedVelocity.magnitude.ToString("G3") + linearSpeedPostfixText;
    }
  }
}
