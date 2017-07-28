using System;
using UnityEngine;
using Leap.Unity.GraphicalRenderer;

namespace Leap.Unity.Recording {

  [AnimationProxy(typeof(PlaybackRuntimeTint))]
  public class RecordRuntimeTint : MonoBehaviour {

    public Color color;

    private void Start() {
      var tintData = GetComponent<LeapGraphic>().GetFeatureData<LeapRuntimeTintData>();
      HierarchyRecorder.OnPreRecordFrame += () => {
        color = tintData.color;
      };
    }
  }
}
