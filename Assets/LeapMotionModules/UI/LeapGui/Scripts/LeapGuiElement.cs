using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LeapGuiElement : MonoBehaviour {

  //[HideInInspector]
  [SerializeField]
  private int _elementId;

  //[HideInInspector]
  [SerializeField]
  public List<LeapGuiElementData> data;

}
