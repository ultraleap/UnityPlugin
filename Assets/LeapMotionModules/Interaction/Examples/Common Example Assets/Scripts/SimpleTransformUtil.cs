using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Examples {

  [AddComponentMenu("")]
  public class SimpleTransformUtil : MonoBehaviour {

    public void SetParentTo(Transform t) {
      this.transform.SetParent(t, true);
    }

    public void ClearParentTransform() {
      this.transform.SetParent(null, true);
    }

  }

}

