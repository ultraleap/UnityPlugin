using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Examples {

  [AddComponentMenu("")]
  public class SimpleScaleUtil : MonoBehaviour {

    public void SetLocalScale(float scale) {
      this.transform.localScale = Vector3.one * scale;
    }

  }

}
