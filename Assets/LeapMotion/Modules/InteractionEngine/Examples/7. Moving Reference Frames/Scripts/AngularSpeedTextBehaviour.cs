/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
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
