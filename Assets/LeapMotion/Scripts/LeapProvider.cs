using UnityEngine;
using System.Collections;

namespace Leap.Unity {
  public abstract class LeapProvider : MonoBehaviour {
    public abstract Frame CurrentFrame { get; }
    public abstract Frame CurrentFixedFrame { get; }
    public abstract Image CurrentImage { get; }
  }
}
