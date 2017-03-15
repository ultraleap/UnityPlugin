using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.LeapPaint.LineDrawing {

  public abstract class Brush3DBase : MonoBehaviour {

    private bool _isBrushing;
    public bool isBrushing { get { return _isBrushing; } }

    public virtual void BeginStroke() {
      _isBrushing = true;
    }

    public virtual void EndStroke() {
      _isBrushing = false;
    }

  }

}