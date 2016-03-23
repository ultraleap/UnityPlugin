using UnityEngine;
using System.Collections;

namespace Leap.Unity {
  /**LeapProvider's supply images and Leap Hands */
  public abstract class LeapProvider : MonoBehaviour {
    public abstract Frame CurrentFrame { get; }
    public abstract Frame CurrentFixedFrame { get; }
    public abstract Image CurrentImage { get; }
  }
}
