using Leap.Unity.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Examples {

  [AddComponentMenu("")]
  public class SimpleMatchAnchorScaleAndState : MonoBehaviour {

    public AnchorableBehaviour anchObj;

    void Update() {
      if (anchObj != null && anchObj.anchor != null && anchObj.isAttached) {
        anchObj.transform.localScale = anchObj.anchor.transform.localScale;

        anchObj.gameObject.SetActive(anchObj.anchor.gameObject.activeInHierarchy);

        if (!anchObj.isActiveAndEnabled) {
          anchObj.transform.position = anchObj.anchor.transform.position;
          if (anchObj.anchorRotation) anchObj.transform.rotation = anchObj.anchor.transform.rotation;
        }
      }
    }

  }

}
