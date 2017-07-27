using UnityEngine;
using Leap.Unity.GraphicalRenderer;

namespace Leap.Unity.Recording {

  [RecordingFriendly]
  public class PlaybackRuntimeTint : MonoBehaviour {

    public Color color;

    private LeapRuntimeTintData _tintData;

    private void Start() {
      _tintData = GetComponent<LeapGraphic>().GetFeatureData<LeapRuntimeTintData>();
    }

    private void Update() {
      _tintData.color = color;
    }
  }
}
