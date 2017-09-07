using System;
using UnityEngine.Playables;

namespace Leap.Unity.Recording {

  [Serializable]
  public class RecordingBehaviour : PlayableBehaviour {
    public LeapRecording recording;
  }
}
