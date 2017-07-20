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
