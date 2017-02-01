using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity.Attributes;

public class LeapGuiBlendShapeData : LeapGuiElementData {

  [SerializeField]
  private BlendShapeType _type = BlendShapeType.Scale;

  [MinValue(0)]
  [SerializeField]
  private float _scaleAmount = 1.1f;

  [SerializeField]
  private Vector3 _translation = new Vector3(0, 0, 0.1f);

  [SerializeField]
  private Vector3 _rotation = new Vector3(0, 0, 5);

  public enum BlendShapeType {
    Translation,
    Rotation,
    Scale,
    Custom
  }
}
