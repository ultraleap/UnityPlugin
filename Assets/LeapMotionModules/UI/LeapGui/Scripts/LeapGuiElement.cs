using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LeapGuiElement : MonoBehaviour {

  [SerializeField]
  public int elementId;

  [SerializeField]
  public AnchorOfConstantSize anchor;

  //[HideInInspector]
  [SerializeField]
  public List<LeapGuiElementData> data;

}
