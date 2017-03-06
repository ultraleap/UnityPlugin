using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.StrokeProcessing {

  public class StrokePoint {

    public Vector3      position;
    public Quaternion   rotation;
    public Vector3      scale;
    public float        deltaTime;
    public Color        color;
    public float        pressure;
    public Object       customData;

    public Vector3 Normal { get { return this.rotation * Vector3.up; } }
    public Vector3 Tangent { get { return this.rotation * Vector3.forward; } }
    public Vector3 Binormal { get { return this.rotation * Vector3.right; } }

  }

}