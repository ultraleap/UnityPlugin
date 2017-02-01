using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class LeapGuiCustomBlendShape : MonoBehaviour {

  protected virtual void Start() { }

  public abstract bool TryGetBlendShape(LeapGuiBlendShapeData data, out Mesh shape);

}
