using System.Collections;
using System.Collections.Generic;
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
