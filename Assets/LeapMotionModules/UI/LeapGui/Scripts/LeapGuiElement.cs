using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LeapGuiElement : MonoBehaviour {

  //Used to ensure that gui elements can be enabled/disabled
  void Start() { }

  [HideInInspector]
  public int elementId;

  [HideInInspector]
  public AnchorOfConstantSize anchor;

  [SerializeField]
  public List<LeapGuiElementData> data;
}
