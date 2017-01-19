using Leap.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAfter(typeof(LeapServiceProvider))]
[ExecuteBefore(typeof(MinimalBody))]
public class SpringBase : MonoBehaviour {

  #region Gizmos

  public static Color GizmoDefaultColor = new Color(0.3F, 0.8F, 0.7F);

  #endregion

}
