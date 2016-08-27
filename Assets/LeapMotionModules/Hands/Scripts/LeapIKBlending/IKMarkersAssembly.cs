using UnityEngine;
using System.Collections;

namespace Leap.Unity {
  public class IKMarkersAssembly : MonoBehaviour {
    public Transform ElbowMarker_L;
    public Transform ElbowMarker_R;
    public Transform ElbowIKTarget_L;
    public Transform ElbowIKTarget_R;
    public Transform RestIKPosition_L;
    public Transform RestIKPosition_R;

    public AnimationCurve DropCurveX;
    public AnimationCurve DropCurveY;
    public AnimationCurve DropCurveZ;
  }
}
