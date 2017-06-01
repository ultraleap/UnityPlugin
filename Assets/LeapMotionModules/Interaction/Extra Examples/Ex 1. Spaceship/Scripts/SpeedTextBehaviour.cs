using Leap.Unity.GraphicalRenderer;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpeedTextBehaviour : MonoBehaviour {

  public LeapTextGraphic textGraphic;

  public string prefixText;

  public Spaceship ship;

  public string postfixText;

  void Update() {
    textGraphic.text = prefixText + ship.shipAlignedVelocity.z.ToString("G3") + postfixText;
  }

}
