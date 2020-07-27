/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Examples {

  [AddComponentMenu("")]
  public class AngularSpeedTextBehaviour : MonoBehaviour {

    public TextMesh textMesh;
    public Spaceship ship;
    public string angularSpeedPrefixText;
    public string angularSpeedPostfixText;

    void Update() {
      textMesh.text = angularSpeedPrefixText + ship.shipAlignedAngularVelocity.magnitude.ToString("G3") + angularSpeedPostfixText;
    }

  }

}
