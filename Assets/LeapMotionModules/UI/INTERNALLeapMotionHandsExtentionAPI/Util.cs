using UnityEngine;

namespace Leap.InternalExtensionAPI {

  public static class Util {

    /// <summary>
    /// Similar to Unity's Transform.LookAt(), but resolves the forward vector of this
    /// Transform to point away from the argument Transform.
    /// 
    /// Useful for billboarding Quads and UI elements whose forward vectors should match
    /// rather than oppose the Main Camera.
    /// </summary>
    public static void LookAwayFrom(this Transform thisTransform, Transform transform) {
      thisTransform.rotation = Quaternion.LookRotation(thisTransform.position - transform.position, Vector3.up);
    }

  }

}